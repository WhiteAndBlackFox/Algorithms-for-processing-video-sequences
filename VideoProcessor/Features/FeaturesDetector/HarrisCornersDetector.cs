using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace VideoProcessor.Features.FeaturesDetector
{
    public class HarrisCornersDetector : ICornersDetector
    {

        private float _k = 0.04f;
        private float _threshold = 1000f;
        private double _sigma = 1.4;
        private int _r = 3;

        //Harris parameter k. Default value is 0.04.
        public float K
        {
            get { return _k; }
            set { _k = value; }
        }

        //Default value is 1000.
        public float Threshold
        {
            get { return _threshold; }
            set { _threshold = value; }
        }

        //Default value is 1.4.
        public double Sigma
        {
            get { return _sigma; }
            set { _sigma = value; }
        }

        //Default value is 3.
        public int Suppression
        {
            get { return _r; }
            set { _r = value; }
        }

        public HarrisCornersDetector()
        {
        }

        public HarrisCornersDetector(float k)
            : this()
        {
            _k = k;
        }

        public HarrisCornersDetector(float k, float threshold)
            : this()
        {
            _k = k;
            _threshold = threshold;
        }

        public HarrisCornersDetector(float k, float threshold, double sigma)
            : this()
        {
            _k = k;
            _threshold = threshold;
            _sigma = sigma;
        }

        public List<IntPoint> ProcessImage(UnmanagedImage image)
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

            grayImage = image.PixelFormat == PixelFormat.Format8bppIndexed ? image : Grayscale.CommonAlgorithms.BT709.Apply(image);


            // get source image size
            int width = grayImage.Width;
            int height = grayImage.Height;
            int stride = grayImage.Stride;
            int offset = stride - width;



            // 1. Calculate partial differences
            UnmanagedImage diffx = UnmanagedImage.Create(width, height, PixelFormat.Format8bppIndexed);
            UnmanagedImage diffy = UnmanagedImage.Create(width, height, PixelFormat.Format8bppIndexed);
            UnmanagedImage diffxy = UnmanagedImage.Create(width, height, PixelFormat.Format8bppIndexed);

            unsafe
            {
                // Compute dx and dy
                byte* src = (byte*)grayImage.ImageData.ToPointer();
                byte* dx = (byte*)diffx.ImageData.ToPointer();
                byte* dy = (byte*)diffy.ImageData.ToPointer();

                // for each line
                for (int y = 0; y < height; y++)
                {
                    // for each pixel
                    for (int x = 0; x < width; x++, src++, dx++, dy++)
                    {
                        // TODO: Place those verifications
                        // outside the innermost loop
                        if (x == 0 || x == width  - 1 ||
                            y == 0 || y == height - 1)
                        {
                            *dx = *dy = 0; continue;
                        }
                                                    
                        int h = -(src[-stride - 1] + src[-1] + src[stride - 1]) +
                                 (src[-stride + 1] + src[+1] + src[stride + 1]);
                        *dx = (byte)(h > 255 ? 255 : h < 0 ? 0 : h);

                        int v = -(src[-stride - 1] + src[-stride] + src[-stride + 1]) +
                                 (src[+stride - 1] + src[+stride] + src[+stride + 1]);
                        *dy = (byte)(v > 255 ? 255 : v < 0 ? 0 : v);
                    }
                    src += offset;
                    dx += offset;
                    dy += offset;
                }


                // Compute dxy
                dx = (byte*)diffx.ImageData.ToPointer();
                var dxy = (byte*)diffxy.ImageData.ToPointer();

                // for each line
                for (int y = 0; y < height; y++)
                {
                    // for each pixel
                    for (int x = 0; x < width; x++, dx++, dxy++)
                    {
                        if (x == 0 || x == width  - 1 ||
                            y == 0 || y == height - 1)
                        {
                            *dxy = 0; continue;
                        }

                        int v = -(dx[-stride - 1] + dx[-stride] + dx[-stride + 1]) +
                                 (dx[+stride - 1] + dx[+stride] + dx[+stride + 1]);
                        *dxy = (byte)(v > 255 ? 255 : v < 0 ? 0 : v);
                    }
                    dx += offset;
                    dxy += offset;
                }
            }


            // 2. Smooth the diff images
            if (_sigma > 0.0)
            {
                GaussianBlur blur = new GaussianBlur(_sigma);
                blur.ApplyInPlace(diffx);
                blur.ApplyInPlace(diffy);
                blur.ApplyInPlace(diffxy);
            }


            // 3. Compute Harris Corner Response
            float[,] H = new float[height, width];

            unsafe
            {
                byte* ptrA = (byte*)diffx.ImageData.ToPointer();
                byte* ptrB = (byte*)diffy.ImageData.ToPointer();
                byte* ptrC = (byte*)diffxy.ImageData.ToPointer();
                float M, A, B, C;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        A = *(ptrA++);
                        B = *(ptrB++);
                        C = *(ptrC++);

                        // Harris corner measure
                        M = (A * B - C * C) - (_k * ((A + B) * (A + B)));

                        if (M > _threshold)
                            H[y, x] = M;
                        else H[y, x] = 0;
                    }

                    ptrA += offset;
                    ptrB += offset;
                    ptrC += offset;
                }
            }


            // Free resources
            diffx.Dispose();
            diffy.Dispose();
            diffxy.Dispose();

            if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                grayImage.Dispose();


            // 4. Suppress non-maximum points
            List<IntPoint> cornersList = new List<IntPoint>();

            // for each row
            for (int y = _r, maxY = height - _r; y < maxY; y++)
            {
                // for each pixel
                for (int x = _r, maxX = width - _r; x < maxX; x++)
                {
                    float currentValue = H[y, x];

                    // for each windows' row
                    for (int i = -_r; (currentValue != 0) && (i <= _r); i++)
                    {
                        // for each windows' pixel
                        for (int j = -_r; j <= _r; j++)
                        {
                            if (H[y + i, x + j] > currentValue)
                            {
                                currentValue = 0;
                                break;
                            }
                        }
                    }

                    // check if this point is really interesting
                    if (currentValue != 0)
                    {
                        cornersList.Add(new IntPoint(x, y));
                    }
                }
            }


            return cornersList;
        }

        public List<IntPoint> ProcessImage(BitmapData imageData)
        {
            return ProcessImage(new UnmanagedImage(imageData));
        }

        public List<IntPoint> ProcessImage(Bitmap image)
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

            List<IntPoint> corners;

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
    }
}
