using System;
using System.Collections.Generic;
using AForge.Imaging;

namespace VideoProcessor.Features.FeaturesDetector
{
    public class SpeededUpRobustFeaturesDescriptor : ICloneable
    {

        private bool _invariant = true;
        private bool _extended;
        private readonly IntegralImage _integral;

        public bool Invariant
        {
            get { return _invariant; }
            set { _invariant = value; }
        }

        public bool Extended
        {
            get { return _extended; }
            set { _extended = value; }
        }

        public IntegralImage Image
        {
            get { return _integral; }
        }

        public SpeededUpRobustFeaturesDescriptor(IntegralImage integralImage)
        {
            _integral = integralImage;
        }

        public void Compute(SpeededUpRobustFeaturePoint point)
        {
            int x = (int)System.Math.Round(point.X, 0);
            int y = (int)System.Math.Round(point.Y, 0);
            int s = (int)System.Math.Round(point.Scale, 0);

            if (_invariant)
            {
                // Get the orientation (for rotation invariance)
                point.Orientation = this.GetOrientation(x, y, s);
            }

            // Extract SURF descriptor
            point.Descriptor = this.GetDescriptor(x, y, s, point.Orientation);
        }

        public void Compute(IEnumerable<SpeededUpRobustFeaturePoint> points)
        {
            foreach (SpeededUpRobustFeaturePoint point in points)
            {
                Compute(point);
            }
        }

        public double GetOrientation(SpeededUpRobustFeaturePoint point)
        {
            // Get rounded feature point data
            int x = (int)Math.Round(point.X, 0);
            int y = (int)Math.Round(point.Y, 0);
            int s = (int)Math.Round(point.Scale, 0);

            // Get the orientation (for rotation invariance)
            return GetOrientation(x, y, s);
        }

        const byte responses = 109;
        readonly double[] resX = new double[responses];
        readonly double[] resY = new double[responses];
        readonly double[] ang = new double[responses];
        static int[] id = { 6, 5, 4, 3, 2, 1, 0, 1, 2, 3, 4, 5, 6 };

        public double GetOrientation(int x, int y, int scale)
        {
            // Calculate Haar responses for points within radius of 6*scale
            for (int i = -6, idx = 0; i <= 6; i++)
            {
                for (int j = -6; j <= 6; j++)
                {
                    if (i * i + j * j < 36)
                    {
                        double g = gauss25[id[i + 6], id[j + 6]];
                        resX[idx] = g * haarX(y + j * scale, x + i * scale, 4 * scale);
                        resY[idx] = g * haarY(y + j * scale, x + i * scale, 4 * scale);
                        ang[idx] = Tools.Angle(resX[idx], resY[idx]);
                        idx++;
                    }
                }
            }

            // Calculate the dominant direction 
            double orientation = 0, max = 0;

            // Loop slides pi/3 window around feature point
            for (double ang1 = 0; ang1 < 2.0 * Math.PI; ang1 += 0.15)
            {
                double ang2 = (ang1 + Math.PI / 3 > 2 * Math.PI ? ang1 - 5 * Math.PI / 3 : ang1 + Math.PI / 3);
                double sumX = 0;
                double sumY = 0;

                for (int k = 0; k < ang.Length; k++)
                {
                    // determine whether the point is within the window
                    if (ang1 < ang2 && ang1 < ang[k] && ang[k] < ang2)
                    {
                        sumX += resX[k];
                        sumY += resY[k];
                    }
                    else if (ang2 < ang1 && ((ang[k] > 0 && ang[k] < ang2) || (ang[k] > ang1 && ang[k] < Math.PI)))
                    {
                        sumX += resX[k];
                        sumY += resY[k];
                    }
                }

                // If the vector produced from this window is longer than all 
                // previous vectors then this forms the new dominant direction
                if (sumX * sumX + sumY * sumY > max)
                {
                    // store largest orientation
                    max = sumX * sumX + sumY * sumY;
                    orientation = Tools.Angle(sumX, sumY);
                }
            }

            // Return orientation of the 
            // dominant response vector
            return orientation;
        }
 
        public double[] GetDescriptor(int x, int y, int scale, double orientation)
        {
            // Determine descriptor size
            double[] descriptor = (this._extended) ? new double[128] : new double[64];

            int count = 0;
            double cos, sin;
            double length = 0;

            double cx = -0.5; // Subregion centers for the
            double cy = +0.0; // 4x4 Gaussian weighting.

            if (!this._invariant)
            {
                cos = 1;
                sin = 0;
            }
            else
            {
                cos = System.Math.Cos(orientation);
                sin = System.Math.Sin(orientation);
            }

            // Calculate descriptor for this interest point
            int i = -8;
            while (i < 12)
            {
                int j = -8;
                i = i - 4;

                cx += 1f;
                cy = -0.5f;

                while (j < 12)
                {
                    cy += 1f;
                    j = j - 4;

                    int ix = i + 5;
                    int jx = j + 5;

                    int xs = (int)System.Math.Round(x + (-jx * scale * sin + ix * scale * cos), 0);
                    int ys = (int)System.Math.Round(y + (+jx * scale * cos + ix * scale * sin), 0);

                    // zero the responses
                    double dx = 0, dy = 0;
                    double mdx = 0, mdy = 0;
                    double dx_yn = 0, dy_xn = 0;
                    double mdx_yn = 0, mdy_xn = 0;

                    for (int k = i; k < i + 9; k++)
                    {
                        for (int l = j; l < j + 9; l++)
                        {
                            // Get coordinates of sample point on the rotated axis
                            int sample_x = (int)System.Math.Round(x + (-l * scale * sin + k * scale * cos), 0);
                            int sample_y = (int)System.Math.Round(y + (+l * scale * cos + k * scale * sin), 0);

                            // Get the Gaussian weighted x and y responses
                            double gauss_s1 = gaussian(xs - sample_x, ys - sample_y, 2.5f * scale);
                            double rx = haarX(sample_y, sample_x, 2 * scale);
                            double ry = haarY(sample_y, sample_x, 2 * scale);

                            // Get the Gaussian weighted x and y responses on rotated axis
                            double rrx = gauss_s1 * (-rx * sin + ry * cos);
                            double rry = gauss_s1 * (rx * cos + ry * sin);


                            if (this._extended)
                            {
                                // split x responses for different signs of y
                                if (rry >= 0)
                                {
                                    dx += rrx;
                                    mdx += System.Math.Abs(rrx);
                                }
                                else
                                {
                                    dx_yn += rrx;
                                    mdx_yn += System.Math.Abs(rrx);
                                }

                                // split y responses for different signs of x
                                if (rrx >= 0)
                                {
                                    dy += rry;
                                    mdy += System.Math.Abs(rry);
                                }
                                else
                                {
                                    dy_xn += rry;
                                    mdy_xn += System.Math.Abs(rry);
                                }
                            }
                            else
                            {
                                dx += rrx;
                                dy += rry;
                                mdx += System.Math.Abs(rrx);
                                mdy += System.Math.Abs(rry);
                            }
                        }
                    }

                    // Add the values to the descriptor vector
                    double gauss_s2 = gaussian(cx - 2.0, cy - 2.0, 1.5);

                    descriptor[count++] = dx * gauss_s2;
                    descriptor[count++] = dy * gauss_s2;
                    descriptor[count++] = mdx * gauss_s2;
                    descriptor[count++] = mdy * gauss_s2;

                    // Add the extended components
                    if (this._extended)
                    {
                        descriptor[count++] = dx_yn * gauss_s2;
                        descriptor[count++] = dy_xn * gauss_s2;
                        descriptor[count++] = mdx_yn * gauss_s2;
                        descriptor[count++] = mdy_xn * gauss_s2;
                    }

                    length += (dx * dx + dy * dy + mdx * mdx + mdy * mdy
                          + dx_yn + dy_xn + mdx_yn + mdy_xn) * gauss_s2 * gauss_s2;

                    j += 9;
                }
                i += 9;
            }

            // Normalize to obtain an unitary vector
            length = System.Math.Sqrt(length);

            if (length > 0)
            {
                for (int d = 0; d < descriptor.Length; ++d)
                    descriptor[d] /= length;
            }

            return descriptor;
        }

        private double haarX(int row, int column, int size)
        {
            double a = _integral.GetRectangleSum(column, row - size / 2,
                column + size / 2 - 1, row - size / 2 + size - 1);

            double b = _integral.GetRectangleSum(column - size / 2, row - size / 2,
                column - size / 2 + size / 2 - 1, row - size / 2 + size - 1);

            return (a - b) / 255.0;
        }

        private double haarY(int row, int column, int size)
        {
            double a = _integral.GetRectangleSum(column - size / 2, row,
                column - size / 2 + size - 1, row + size / 2 - 1);

            double b = _integral.GetRectangleSum(column - size / 2, row - size / 2,
                column - size / 2 + size - 1, row - size / 2 + size / 2 - 1);

            return (a - b) / 255.0;
        }



        #region Gaussian calculation

        /// <summary>
        ///   Get the value of the Gaussian with std dev sigma at the point (x,y)
        /// </summary>
        /// 
        private static double gaussian(int x, int y, double sigma)
        {
            return (1.0 / (2.0 * Math.PI * sigma * sigma)) * System.Math.Exp(-(x * x + y * y) / (2.0f * sigma * sigma));
        }

        /// <summary>
        ///   Get the value of the Gaussian with std dev sigma at the point (x,y)
        /// </summary>
        private static double gaussian(double x, double y, double sigma)
        {
            return 1.0 / (2.0 * Math.PI * sigma * sigma) * System.Math.Exp(-(x * x + y * y) / (2.0f * sigma * sigma));
        }

        /// <summary>
        ///   Gaussian look-up table for sigma = 2.5
        /// </summary>
        /// 
        private static readonly double[,] gauss25 = 
        {
            { 0.02350693969273, 0.01849121369071, 0.01239503121241, 0.00708015417522, 0.00344628101733, 0.00142945847484, 0.00050524879060 },
            { 0.02169964028389, 0.01706954162243, 0.01144205592615, 0.00653580605408, 0.00318131834134, 0.00131955648461, 0.00046640341759 },
            { 0.01706954162243, 0.01342737701584, 0.00900063997939, 0.00514124713667, 0.00250251364222, 0.00103799989504, 0.00036688592278 },
            { 0.01144205592615, 0.00900063997939, 0.00603330940534, 0.00344628101733, 0.00167748505986, 0.00069579213743, 0.00024593098864 },
            { 0.00653580605408, 0.00514124713667, 0.00344628101733, 0.00196854695367, 0.00095819467066, 0.00039744277546, 0.00014047800980 },
            { 0.00318131834134, 0.00250251364222, 0.00167748505986, 0.00095819467066, 0.00046640341759, 0.00019345616757, 0.00006837798818 },
            { 0.00131955648461, 0.00103799989504, 0.00069579213743, 0.00039744277546, 0.00019345616757, 0.00008024231247, 0.00002836202103 }
        };

        #endregion



        #region ICloneable Members

        /// <summary>
        ///   Creates a new object that is a copy of the current instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A new object that is a copy of this instance.
        /// </returns>
        /// 
        public object Clone()
        {
            var clone = new SpeededUpRobustFeaturesDescriptor(_integral);
            clone._extended = _extended;
            clone._invariant = _invariant;

            return clone;
        }

        #endregion
    }
}
