using System;
using System.Drawing;
using System.Text;
using VideoProcessor.Features.Base;

namespace VideoProcessor.Features.FeaturesDetector
{
    [Serializable]
    public class ScaleInvariantFeatureTranformKeypoint : IFeaturePoint<byte[]>
    {

        public ScaleInvariantFeatureTranformKeypoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }

        public double Y { get; set; }
 
        public double Scale { get; set; }

        public double Orientation { get; set; }

        public byte[] Descriptor { get; set; }

        public string ToHex()
        {
            if (Descriptor == null)
                return String.Empty;

            StringBuilder hex = new StringBuilder(Descriptor.Length * 2);
            foreach (byte b in Descriptor)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public string ToBinary()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Descriptor.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    bool set = (Descriptor[i] & (1 << j)) != 0;
                    sb.Append(set ? "1" : "0");
                }
            }

            return sb.ToString();
        }

        public string ToBase64()
        {
            if (Descriptor == null)
                return String.Empty;

            return Convert.ToBase64String(Descriptor);
        }
        
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

        public static implicit operator Point(ScaleInvariantFeatureTranformKeypoint point)
        {
            return point.ToPoint();
        }

        public static implicit operator PointF(ScaleInvariantFeatureTranformKeypoint point)
        {
            return point.ToPointF();
        }

        public static implicit operator AForge.IntPoint(ScaleInvariantFeatureTranformKeypoint point)
        {
            return point.ToIntPoint();
        }
    }
}
