using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace VideoProcessor.Features.FeaturesDetector
{
    public enum ScaleInvariantFeatureTranformKeypointDescriptorType
    {
        None,
        Standard,
        Extended,
    }

    [Serializable]
    public class ScaleInvariantFeatureTranform
    {

        private ScaleInvariantFeatureTranformKeypointDescriptorType _featureType = ScaleInvariantFeatureTranformKeypointDescriptorType.Standard;
        private float _scale = 22.0f;
        private int _octaves = 4;


        [NonSerialized]
        private IntegralImage _integral;

        [NonSerialized]
        private UnmanagedImage _grayImage;

        [NonSerialized]
        private ScaleInvariantFeatureTranformKeypointPattern _pattern;

        [NonSerialized]
        private ScaleInvariantFeatureTranformKeypointDescriptor _descriptor;

        public ICornersDetector Detector { get; private set; }

        public ScaleInvariantFeatureTranformKeypointDescriptorType ComputeDescriptors
        {
            get { return _featureType; }
            set { _featureType = value; }
        }

        public int Octaves
        {
            get { return _pattern.Octaves; }
            set
            {
                if (value != _octaves)
                {
                    _octaves = value;
                    _pattern = null;
                }
            }
        }

        public float Scale
        {
            get { return _pattern.Scale; }
            set
            {
                if (value != _scale)
                {
                    _scale = value;
                    _pattern = null;
                }
            }
        }

        public ScaleInvariantFeatureTranform(int threshold)
        {
            init(new FastCornersDetector(threshold));
        }

        public ScaleInvariantFeatureTranform()
        {
            init(new FastCornersDetector());
        }

        public ScaleInvariantFeatureTranform(ICornersDetector detector)
        {
            init(detector);
        }

        private void init(ICornersDetector detector)
        {
            Detector = detector;
        }

        public List<ScaleInvariantFeatureTranformKeypoint> ProcessImage(UnmanagedImage image)
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
            if (image.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                _grayImage = image;
            }
            else
            {
                // create temporary grayscale image
                _grayImage = Grayscale.CommonAlgorithms.BT709.Apply(image);
            }


            // 1. Extract corners points from the image.
            List<IntPoint> corners = Detector.ProcessImage(_grayImage);

            List<ScaleInvariantFeatureTranformKeypoint> features = new List<ScaleInvariantFeatureTranformKeypoint>();
            for (int i = 0; i < corners.Count; i++)
                features.Add(new ScaleInvariantFeatureTranformKeypoint(corners[i].X, corners[i].Y));


            // 2. Compute the integral for the given image
            _integral = IntegralImage.FromBitmap(_grayImage);


            // 3. Compute feature descriptors if required
            _descriptor = null;
            if (_featureType != ScaleInvariantFeatureTranformKeypointDescriptorType.None)
            {
                _descriptor = GetDescriptor();
                _descriptor.Compute(features);
            }

            return features;
        }

        public ScaleInvariantFeatureTranformKeypointDescriptor GetDescriptor()
        {
            if (_descriptor == null || _pattern == null)
            {
                if (_pattern == null)
                    _pattern = new ScaleInvariantFeatureTranformKeypointPattern(_octaves, _scale);

                _descriptor = new ScaleInvariantFeatureTranformKeypointDescriptor(_grayImage, _integral, _pattern);
                _descriptor.Extended = _featureType == ScaleInvariantFeatureTranformKeypointDescriptorType.Extended;
            }

            return _descriptor;
        }

        public List<ScaleInvariantFeatureTranformKeypoint> ProcessImage(Bitmap image)
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

            List<ScaleInvariantFeatureTranformKeypoint> corners;

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

        public List<ScaleInvariantFeatureTranformKeypoint> ProcessImage(BitmapData imageData)
        {
            return ProcessImage(new UnmanagedImage(imageData));
        }
    }
}
