using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using VideoProcessor.Features.Base;

namespace VideoProcessor.Features.FeaturesDetector
{
    public enum SpeededUpRobustFeatureDescriptorType
    {
        None,
        Standard,
        Extended,
    }

    [Serializable]
    public class SpeededUpRobustFeaturesDetector : ICornersDetector, IFeatureDetector<SpeededUpRobustFeaturePoint>
    {
        private int _octaves = 5;
        private int _initial = 2;

        private double _threshold;

        [NonSerialized]
        private ResponseLayerCollection _responses;

        [NonSerialized]
        private IntegralImage _integral;

        [NonSerialized]
        private SpeededUpRobustFeaturesDescriptor _descriptor;
        private SpeededUpRobustFeatureDescriptorType _featureType = SpeededUpRobustFeatureDescriptorType.Standard;
        private bool _computeOrientation = true;


        #region Constructors
        public SpeededUpRobustFeaturesDetector()
            : this(0.0002f)
        {
        }

        public SpeededUpRobustFeaturesDetector(float threshold)
            : this(threshold, 5, 2)
        {
        }

        public SpeededUpRobustFeaturesDetector(float threshold, int octaves, int initial)
        {
            _threshold = threshold;
            _octaves = octaves;
            _initial = initial;
        }
        #endregion

        #region Properties

        public bool ComputeOrientation
        {
            get { return _computeOrientation; }
            set { _computeOrientation = value; }
        }

        public SpeededUpRobustFeatureDescriptorType ComputeDescriptors
        {
            get { return _featureType; }
            set { _featureType = value; }
        }

        public double Threshold
        {
            get { return _threshold; }
            set { _threshold = value; }
        }

        public int Octaves
        {
            get { return _octaves; }
            set
            {
                if (_octaves != value)
                {
                    _octaves = value;
                    _responses = null;
                }
            }
        }

        public int Step
        {
            get { return _initial; }
            set
            {
                if (_initial != value)
                {
                    _initial = value;
                    _responses = null;
                }
            }
        }
        #endregion

        public List<SpeededUpRobustFeaturePoint> ProcessImage(UnmanagedImage image)
        {
            // check image format
            if (
                (image.PixelFormat != PixelFormat.Format8bppIndexed) &&
                (image.PixelFormat != PixelFormat.Format24bppRgb) &&
                (image.PixelFormat != PixelFormat.Format32bppRgb) &&
                (image.PixelFormat != PixelFormat.Format32bppArgb)
                )
            {
                throw new UnsupportedImageFormatException("Unsupported pixel format of the source image.");
            }

            // make sure we have grayscale image
            UnmanagedImage grayImage = null;

            if (image.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                grayImage = image;
            }
            else
            {
                // create temporary grayscale image
                grayImage = Grayscale.CommonAlgorithms.BT709.Apply(image);
            }


            // 1. Compute the integral for the given image
            _integral = IntegralImage.FromBitmap(grayImage);



            // 2. Create and compute interest point response map
            if (_responses == null)
            {
                // re-create only if really needed
                _responses = new ResponseLayerCollection(image.Width, image.Height, _octaves, _initial);
            }
            else
            {
                _responses.Update(image.Width, image.Height, _initial);
            }

            // Compute the response map
            _responses.Compute(_integral);


            // 3. Suppress non-maximum points
            List<SpeededUpRobustFeaturePoint> featureList =
                new List<SpeededUpRobustFeaturePoint>();

            // for each image pyramid in the response map
            foreach (ResponseLayer[] layers in _responses)
            {
                // Grab the three layers forming the pyramid
                ResponseLayer bot = layers[0]; // bottom layer
                ResponseLayer mid = layers[1]; // middle layer
                ResponseLayer top = layers[2]; // top layer

                int border = (top.Size + 1) / (2 * top.Step);

                int tstep = top.Step;
                int mstep = mid.Size - bot.Size;

                int mscale = mid.Width / top.Width;
                int bscale = bot.Width / top.Width;

                int r = 1;

                // for each row
                for (int y = border + 1; y < top.Height - border; y++)
                {
                    // for each pixel
                    for (int x = border + 1; x < top.Width - border; x++)
                    {
                        double currentValue = mid.Responses[y * mscale, x * mscale];

                        // for each windows' row
                        for (int i = -r; (currentValue >= _threshold) && (i <= r); i++)
                        {
                            // for each windows' pixel
                            for (int j = -r; j <= r; j++)
                            {
                                int yi = y + i;
                                int xj = x + j;

                                // for each response layer
                                if (top.Responses[yi, xj] >= currentValue ||
                                    bot.Responses[yi * bscale, xj * bscale] >= currentValue || ((i != 0 || j != 0) &&
                                    mid.Responses[yi * mscale, xj * mscale] >= currentValue))
                                {
                                    currentValue = 0;
                                    break;
                                }
                            }
                        }

                        // check if this point is really interesting
                        if (currentValue >= _threshold)
                        {
                            // interpolate to sub-pixel precision
                            double[] offset = interpolate(y, x, top, mid, bot);

                            if (System.Math.Abs(offset[0]) < 0.5 &&
                                System.Math.Abs(offset[1]) < 0.5 &&
                                System.Math.Abs(offset[2]) < 0.5)
                            {
                                featureList.Add(new SpeededUpRobustFeaturePoint(
                                    (x + offset[0]) * tstep,
                                    (y + offset[1]) * tstep,
                                    0.133333333 * (mid.Size + offset[2] * mstep),
                                    mid.Laplacian[y * mscale, x * mscale]));
                            }
                        }

                    }
                }
            }

            _descriptor = null;

            if (_featureType != SpeededUpRobustFeatureDescriptorType.None)
            {
                _descriptor = new SpeededUpRobustFeaturesDescriptor(_integral);
                _descriptor.Extended = _featureType == SpeededUpRobustFeatureDescriptorType.Extended;
                _descriptor.Invariant = _computeOrientation;
                _descriptor.Compute(featureList);
            }
            else if (_computeOrientation)
            {
                _descriptor = new SpeededUpRobustFeaturesDescriptor(_integral);
                foreach (var p in featureList) p.Orientation = _descriptor.GetOrientation(p);
            }

            return featureList;
        }

        public SpeededUpRobustFeaturesDescriptor GetDescriptor()
        {
            if (_descriptor == null)
            {
                _descriptor = new SpeededUpRobustFeaturesDescriptor(_integral);
                _descriptor.Extended = _featureType == SpeededUpRobustFeatureDescriptorType.Extended;
                _descriptor.Invariant = _computeOrientation;
            }

            return _descriptor;
        }

        public List<SpeededUpRobustFeaturePoint> ProcessImage(BitmapData imageData)
        {
            return ProcessImage(new UnmanagedImage(imageData));
        }

        public List<SpeededUpRobustFeaturePoint> ProcessImage(Bitmap image)
        {
            // check image format
            if (
                (image.PixelFormat != PixelFormat.Format8bppIndexed) &&
                (image.PixelFormat != PixelFormat.Format24bppRgb) &&
                (image.PixelFormat != PixelFormat.Format32bppRgb) &&
                (image.PixelFormat != PixelFormat.Format32bppArgb)
                )
            {
                throw new UnsupportedImageFormatException("Unsupported pixel format of the source");
            }

            // lock source image
            BitmapData imageData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);

            List<SpeededUpRobustFeaturePoint> corners;

            try
            {
                // process the image
                corners = ProcessImage(new UnmanagedImage(imageData));
            }
            finally
            {
                // unlock image
                image.UnlockBits(imageData);
            }

            return corners;
        }


        private static double[] interpolate(int y, int x, ResponseLayer top, ResponseLayer mid, ResponseLayer bot)
        {
            int bs = bot.Width / top.Width;
            int ms = mid.Width / top.Width;
            int xp1 = x + 1, yp1 = y + 1;
            int xm1 = x - 1, ym1 = y - 1;

            // Compute first order scale-space derivatives
            double dx = (mid.Responses[y * ms, xp1 * ms] - mid.Responses[y * ms, xm1 * ms]) / 2f;
            double dy = (mid.Responses[yp1 * ms, x * ms] - mid.Responses[ym1 * ms, x * ms]) / 2f;
            double ds = (top.Responses[y, x] - bot.Responses[y * bs, x * bs]) / 2f;

            double[] d = 
            { 
                -dx,
                -dy,
                -ds
            };

            // Compute Hessian
            double v = mid.Responses[y * ms, x * ms] * 2.0;
            double dxx = (mid.Responses[y * ms, xp1 * ms] + mid.Responses[y * ms, xm1 * ms] - v);
            double dyy = (mid.Responses[yp1 * ms, x * ms] + mid.Responses[ym1 * ms, x * ms] - v);
            double dxs = (top.Responses[y, xp1] - top.Responses[y, x - 1] - bot.Responses[y * bs, xp1 * bs] + bot.Responses[y * bs, xm1 * bs]) / 4f;
            double dys = (top.Responses[yp1, x] - top.Responses[y - 1, x] - bot.Responses[yp1 * bs, x * bs] + bot.Responses[ym1 * bs, x * bs]) / 4f;
            double dss = (top.Responses[y, x] + bot.Responses[y * ms, x * ms] - v);
            double dxy = (mid.Responses[yp1 * ms, xp1 * ms] - mid.Responses[yp1 * ms, xm1 * ms]
                - mid.Responses[ym1 * ms, xp1 * ms] + mid.Responses[ym1 * ms, xm1 * ms]) / 4f;

            double[,] H =
            {
                { dxx, dxy, dxs },
                { dxy, dyy, dys },
                { dxs, dys, dss },
            };

            // Compute interpolation offsets
            return H.Inverse(true).Multiply(d);
        }

        #region ICornersDetector Members

        List<IntPoint> ICornersDetector.ProcessImage(UnmanagedImage image)
        {
            return ProcessImage(image).ConvertAll(p => new IntPoint((int)p.X, (int)p.Y));
        }

        List<IntPoint> ICornersDetector.ProcessImage(BitmapData imageData)
        {
            return ProcessImage(imageData).ConvertAll(p => new IntPoint((int)p.X, (int)p.Y));
        }

        List<IntPoint> ICornersDetector.ProcessImage(Bitmap image)
        {
            return ProcessImage(image).ConvertAll(p => new IntPoint((int)p.X, (int)p.Y));
        }
        #endregion
    }
}
