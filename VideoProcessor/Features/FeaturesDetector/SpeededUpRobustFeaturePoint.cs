using System;
using System.Drawing;
using VideoProcessor.Features.Base;

namespace VideoProcessor.Features.FeaturesDetector
{
    [Serializable]
    public class SpeededUpRobustFeaturePoint : IFeaturePoint
    {
        public SpeededUpRobustFeaturePoint(double x, double y, double scale, int laplacian)
        {
            X = x;
            Y = y;
            Scale = scale;
            Laplacian = laplacian;
        }

        public SpeededUpRobustFeaturePoint(double x, double y, double scale, int laplacian,
            double orientation, double response)
        {
            X = x;
            Y = y;
            Scale = scale;
            Laplacian = laplacian;
            Orientation = orientation;
            Response = response;
        }

        public SpeededUpRobustFeaturePoint(double x, double y, double scale, int laplacian,
            double orientation, double response, double[] descriptor)
        {
            X = x;
            Y = y;
            Scale = scale;
            Laplacian = laplacian;
            Orientation = orientation;
            Response = response;
            Descriptor = descriptor;
        }


        public double X { get; set; }

        public double Y { get; set; }

        public double Scale { get; set; }

        public double Response { get; set; }

        public double Orientation { get; set; }

        public int Laplacian { get; set; }

        public double[] Descriptor { get; set; }

        public AForge.IntPoint ToIntPoint()
        {
            return new AForge.IntPoint((int)X, (int)Y);
        }

        public Point ToPoint()
        {
            return new Point((int)X, (int)Y);
        }

        public PointF ToPointF()
        {
            return new PointF((float)X, (float)Y);
        }

        public static implicit operator Point(SpeededUpRobustFeaturePoint point)
        {
            return point.ToPoint();
        }

        public static implicit operator PointF(SpeededUpRobustFeaturePoint point)
        {
            return point.ToPointF();
        }

        public static implicit operator AForge.IntPoint(SpeededUpRobustFeaturePoint point)
        {
            return point.ToIntPoint();
        }
    }
}
