using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge;
using AForge.Imaging;
using VideoProcessor.Decompositions;

namespace VideoProcessor.Features
{
    public static class Tools
    {
        private const double Sqrt2 = 1.4142135623730951;
        private static Random _random = new Random(new DateTime().Millisecond);

        #region Algebra and geometry tools

        /// <summary>
        ///   Creates an homography matrix matching points
        ///   from a set of points to another.
        /// </summary>
        public static MatrixH Homography(PointH[] points1, PointH[] points2)
        {
            // Initial argument checkings
            if (points1.Length != points2.Length)
                throw new ArgumentException("The number of points should be equal.");

            if (points1.Length < 4)
                throw new ArgumentException("At least four points are required to fit an homography");


            int N = points1.Length;

            MatrixH T1, T2; // Normalize input points
            points1 = points1.Normalize(out T1);
            points2 = points2.Normalize(out T2);

            // Create the matrix A
            double[,] A = new double[3 * N, 9];
            for (int i = 0; i < N; i++)
            {
                PointH X = points1[i];
                double x = points2[i].X;
                double y = points2[i].Y;
                double w = points2[i].W;
                int r = 3 * i;

                A[r, 0] = 0;
                A[r, 1] = 0;
                A[r, 2] = 0;
                A[r, 3] = -w * X.X;
                A[r, 4] = -w * X.Y;
                A[r, 5] = -w * X.W;
                A[r, 6] = y * X.X;
                A[r, 7] = y * X.Y;
                A[r, 8] = y * X.W;

                r++;
                A[r, 0] = w * X.X;
                A[r, 1] = w * X.Y;
                A[r, 2] = w * X.W;
                A[r, 3] = 0;
                A[r, 4] = 0;
                A[r, 5] = 0;
                A[r, 6] = -x * X.X;
                A[r, 7] = -x * X.Y;
                A[r, 8] = -x * X.W;

                r++;
                A[r, 0] = -y * X.X;
                A[r, 1] = -y * X.Y;
                A[r, 2] = -y * X.W;
                A[r, 3] = x * X.X;
                A[r, 4] = x * X.Y;
                A[r, 5] = x * X.W;
                A[r, 6] = 0;
                A[r, 7] = 0;
                A[r, 8] = 0;
            }


            // Create the singular value decomposition
            SingularValueDecomposition svd = new SingularValueDecomposition(A, false, true);
            double[,] V = svd.RightSingularVectors;


            // Extract the homography matrix
            MatrixH H = new MatrixH((float)V[0, 8], (float)V[1, 8], (float)V[2, 8],
                                    (float)V[3, 8], (float)V[4, 8], (float)V[5, 8],
                                    (float)V[6, 8], (float)V[7, 8], (float)V[8, 8]);

            // Denormalize
            H = T2.Inverse().Multiply(H.Multiply(T1));

            return H;
        }

        public static MatrixH Homography(PointF[] points1, PointF[] points2)
        {
            // Initial argument checkings
            if (points1.Length != points2.Length)
                throw new ArgumentException("The number of points should be equal.");

            if (points1.Length < 4)
                throw new ArgumentException("At least four points are required to fit an homography");


            int N = points1.Length;

            MatrixH T1, T2; // Normalize input points
            points1 = Tools.Normalize(points1, out T1);
            points2 = Tools.Normalize(points2, out T2);

            // Create the matrix A
            double[,] A = new double[3 * N, 9];
            for (int i = 0; i < N; i++)
            {
                PointF X = points1[i];
                double x = points2[i].X;
                double y = points2[i].Y;
                int r = 3 * i;

                A[r, 0] = 0;
                A[r, 1] = 0;
                A[r, 2] = 0;
                A[r, 3] = -X.X;
                A[r, 4] = -X.Y;
                A[r, 5] = -1;
                A[r, 6] = y * X.X;
                A[r, 7] = y * X.Y;
                A[r, 8] = y;

                r++;
                A[r, 0] = X.X;
                A[r, 1] = X.Y;
                A[r, 2] = 1;
                A[r, 3] = 0;
                A[r, 4] = 0;
                A[r, 5] = 0;
                A[r, 6] = -x * X.X;
                A[r, 7] = -x * X.Y;
                A[r, 8] = -x;

                r++;
                A[r, 0] = -y * X.X;
                A[r, 1] = -y * X.Y;
                A[r, 2] = -y;
                A[r, 3] = x * X.X;
                A[r, 4] = x * X.Y;
                A[r, 5] = x;
                A[r, 6] = 0;
                A[r, 7] = 0;
                A[r, 8] = 0;
            }


            // Create the singular value decomposition
            SingularValueDecomposition svd = new SingularValueDecomposition(A, false, true);
            double[,] V = svd.RightSingularVectors;


            // Extract the homography matrix
            MatrixH H = new MatrixH((float)V[0, 8], (float)V[1, 8], (float)V[2, 8],
                                    (float)V[3, 8], (float)V[4, 8], (float)V[5, 8],
                                    (float)V[6, 8], (float)V[7, 8], (float)V[8, 8]);

            // Denormalize
            H = T2.Inverse().Multiply(H.Multiply(T1));

            return H;
        }

        /// <summary>
        ///   Normalizes a set of homogeneous points so that the origin is located
        ///   at the centroid and the mean distance to the origin is sqrt(2).
        /// </summary>
        public static PointH[] Normalize(this PointH[] points, out MatrixH T)
        {
            float n = points.Length;
            float xmean = 0, ymean = 0;
            for (int i = 0; i < points.Length; i++)
            {
                points[i].X = points[i].X / points[i].W;
                points[i].Y = points[i].Y / points[i].W;
                points[i].W = 1;

                xmean += points[i].X;
                ymean += points[i].Y;
            }
            xmean /= n; ymean /= n;


            float scale = 0;
            foreach (PointH point in points)
            {
                float x = point.X - xmean;
                float y = point.Y - ymean;

                scale += (float)Math.Sqrt(x * x + y * y);
            }

            scale = (float)(Sqrt2 * n / scale);


            T = new MatrixH
                (
                    scale, 0, -scale * xmean,
                    0, scale, -scale * ymean,
                    0, 0, 1
                );

            return T.TransformPoints(points);
        }

        /// <summary>
        ///   Normalizes a set of homogeneous points so that the origin is located
        ///   at the centroid and the mean distance to the origin is sqrt(2).
        /// </summary>
        public static PointF[] Normalize(this PointF[] points, out MatrixH T)
        {
            float n = points.Length;
            float xmean = 0, ymean = 0;
            for (int i = 0; i < points.Length; i++)
            {
                points[i].X = points[i].X;
                points[i].Y = points[i].Y;

                xmean += points[i].X;
                ymean += points[i].Y;
            }
            xmean /= n; ymean /= n;


            float scale = 0;
            foreach (PointF point in points)
            {
                float x = point.X - xmean;
                float y = point.Y - ymean;

                scale += (float)Math.Sqrt(x * x + y * y);
            }

            scale = (float)(Sqrt2 * n / scale);


            T = new MatrixH
                (
                    scale, 0, -scale * xmean,
                    0, scale, -scale * ymean,
                    0, 0, 1
                );

            return T.TransformPoints(points);
        }

        /// <summary>
        ///   Detects if three points are colinear.
        /// </summary>
        public static bool Colinear(PointF pt1, PointF pt2, PointF pt3)
        {
            return Math.Abs(
                 (pt1.Y - pt2.Y) * pt3.X +
                 (pt2.X - pt1.X) * pt3.Y +
                 (pt1.X * pt2.Y - pt1.Y * pt2.X)) < Special.SingleEpsilon;
        }

        /// <summary>
        ///   Detects if three points are colinear.
        /// </summary>
        public static bool Colinear(PointH pt1, PointH pt2, PointH pt3)
        {
            return Math.Abs(
             (pt1.Y * pt2.W - pt1.W * pt2.Y) * pt3.X +
             (pt1.W * pt2.X - pt1.X * pt2.W) * pt3.Y +
             (pt1.X * pt2.Y - pt1.Y * pt2.X) * pt3.W) < Special.SingleEpsilon;
        }
        #endregion

        #region Image tools
        /// <summary>
        ///   Computes the sum of the pixels in a given image.
        /// </summary>
        public static int Sum(this BitmapData image)
        {
            if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new UnsupportedImageFormatException("Only grayscale images are supported");

            int width = image.Width;
            int height = image.Height;
            int offset = image.Stride - image.Width;

            int sum = 0;

            unsafe
            {
                byte* src = (byte*)image.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, src++)
                        sum += (*src);
                    src += offset;
                }
            }

            return sum;
        }

        /// <summary>
        ///   Computes the sum of the pixels in a given image.
        /// </summary>
        public static int Sum(this BitmapData image, Rectangle rectangle)
        {
            if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new UnsupportedImageFormatException("Only grayscale images are supported");

            int stride = image.Stride;
            int rwidth = rectangle.Width;
            int rheight = rectangle.Height;
            int rx = rectangle.X;
            int ry = rectangle.Y;

            int sum = 0;

            unsafe
            {
                byte* src = (byte*)image.Scan0.ToPointer();

                for (int y = 0; y < rheight; y++)
                {
                    byte* p = src + stride * (ry + y) + rx;

                    for (int x = 0; x < rwidth; x++)
                        sum += (*p++);
                }
            }

            return sum;
        }

        /// <summary>
        ///   Computes the sum of the pixels in a given image.
        /// </summary>
        public static int Sum(this Bitmap image)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);

            int sum = Sum(data);

            image.UnlockBits(data);

            return sum;
        }

        /// <summary>
        ///   Computes the sum of the pixels in a given image.
        /// </summary>
        public static int Sum(this Bitmap image, Rectangle rectangle)
        {
            BitmapData data = image.LockBits(rectangle,
                ImageLockMode.ReadOnly, image.PixelFormat);

            int sum = Sum(data);

            image.UnlockBits(data);

            return sum;
        }

        /// <summary>
        ///   Converts a given image into a array of double-precision
        ///   floating-point numbers scaled between -1 and 1.
        /// </summary>
        public static double[] ToDoubleArray(this Bitmap image, int channel)
        {
            return ToDoubleArray(image, channel, -1, 1);
        }

        /// <summary>
        ///   Converts a given image into a array of double-precision
        ///   floating-point numbers scaled between the given range.
        /// </summary>
        public static double[] ToDoubleArray(this Bitmap image, int channel, double min, double max)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            double[] array = ToDoubleArray(data, channel, min, max);
            image.UnlockBits(data);
            return array;
        }

        /// <summary>
        ///   Converts a given image into a array of double-precision
        ///   floating-point numbers scaled between -1 and 1.
        /// </summary>
        public static double[] ToDoubleArray(this BitmapData image, int channel)
        {
            return ToDoubleArray(image, channel, -1, 1);
        }

        /// <summary>
        ///   Converts a given image into a array of double-precision
        ///   floating-point numbers scaled between the given range.
        /// </summary>
        public static double[] ToDoubleArray(this BitmapData image, int channel, double min, double max)
        {
            int width = image.Width;
            int height = image.Height;
            int offset = image.Stride - image.Width;

            double[] data = new double[width * height];
            int dst = 0;

            unsafe
            {
                byte* src = (byte*)image.Scan0.ToPointer() + channel;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, src++, dst++)
                    {
                        data[dst] = Tools.Scale(0, 255, min, max, *src);
                    }
                    src += offset;
                }
            }

            return data;
        }

        /// <summary>
        ///   Converts a given image into a array of double-precision
        ///   floating-point numbers scaled between -1 and 1.
        /// </summary>
        public static double[][] ToDoubleArray(this Bitmap image)
        {
            return ToDoubleArray(image, -1, 1);
        }

        /// <summary>
        ///   Converts a given image into a array of double-precision
        ///   floating-point numbers scaled between the given range.
        /// </summary>
        public static double[][] ToDoubleArray(this Bitmap image, double min, double max)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);

            double[][] array = ToDoubleArray(data, min, max);

            image.UnlockBits(data);

            return array;
        }

        /// <summary>
        ///   Converts a given image into a array of double-precision
        ///   floating-point numbers scaled between the given range.
        /// </summary>
        public static double[][] ToDoubleArray(this BitmapData image, double min, double max)
        {
            int width = image.Width;
            int height = image.Height;
            int pixelSize = System.Drawing.Image.GetPixelFormatSize(image.PixelFormat) / 8;
            int offset = image.Stride - image.Width * pixelSize;

            double[][] data = new double[width * height][];
            int dst = 0;

            unsafe
            {
                byte* src = (byte*)image.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, dst++)
                    {
                        double[] pixel = data[dst] = new double[pixelSize];
                        for (int i = pixel.Length-1; i >= 0; i--, src++)
                            pixel[i] = Tools.Scale(0, 255, min, max, *src);
                    }
                    src += offset;
                }
            }

            return data;
        }
        #endregion

        #region Conversions

        /// <summary>
        ///   Converts an image given as a array of pixel values into
        ///   a <see cref="System.Drawing.Bitmap"/>.
        /// </summary>
        /// <param name="pixels">An array containing the grayscale pixel
        /// values as <see cref="System.Double">doubles</see>.</param>
        /// <param name="width">The width of the resulting image.</param>
        /// <param name="height">The height of the resulting image.</param>
        /// <param name="min">The minimum value representing a color value of 0.</param>
        /// <param name="max">The maximum value representing a color value of 255. </param>
        /// <returns>A <see cref="System.Drawing.Bitmap"/> of given width and height
        /// containing the given pixel values.</returns>
        public static Bitmap ToBitmap(this double[] pixels, int width, int height, double min, double max)
        {
            Bitmap bitmap = AForge.Imaging.Image.CreateGrayscaleImage(width, height);

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, bitmap.PixelFormat);

            int offset = data.Stride - width;
            int src = 0;

            unsafe
            {
                byte* dst = (byte*)data.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, src++, dst++)
                    {
                        *dst = (byte)Tools.Scale(min, max, 0, 255, pixels[src]);
                    }
                    dst += offset;
                }
            }

            bitmap.UnlockBits(data);

            return bitmap;
        }

        /// <summary>
        ///   Converts an image given as a array of pixel values into
        ///   a <see cref="System.Drawing.Bitmap"/>.
        /// </summary>
        /// <param name="pixels">An jagged array containing the pixel values
        /// as double arrays. Each element of the arrays will be converted to
        /// a R, G, B, A value. The bits per pixel of the resulting image
        /// will be set according to the size of these arrays.</param>
        /// <param name="width">The width of the resulting image.</param>
        /// <param name="height">The height of the resulting image.</param>
        /// <param name="min">The minimum value representing a color value of 0.</param>
        /// <param name="max">The maximum value representing a color value of 255. </param>
        /// <returns>A <see cref="System.Drawing.Bitmap"/> of given width and height
        /// containing the given pixel values.</returns>
        public static Bitmap ToBitmap(this double[][] pixels, int width, int height, double min, double max)
        {
            PixelFormat format;
            int channels = pixels[0].Length;

            switch (channels)
            {
                case 1:
                    format = PixelFormat.Format8bppIndexed;
                    break;

                case 3:
                    format = PixelFormat.Format24bppRgb;
                    break;

                case 4:
                    format = PixelFormat.Format32bppArgb;
                    break;

                default:
                    throw new ArgumentException("pixels");
            }


            Bitmap bitmap = new Bitmap(width, height, format);

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, format);

            int pixelSize = System.Drawing.Image.GetPixelFormatSize(format) / 8;
            int offset = data.Stride - width * pixelSize;
            int src = 0;

            unsafe
            {
                byte* dst = (byte*)data.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, src++)
                    {
                        for (int c = channels - 1; c >= 0; c--, dst++)
                        {
                            *dst = (byte)Tools.Scale(min, max, 0, 255, pixels[src][c]);
                        }
                    }
                    dst += offset;
                }
            }

            bitmap.UnlockBits(data);

            return bitmap;
        }

        /// <summary>
        ///   Converts an image given as a array of pixel values into
        ///   a <see cref="System.Drawing.Bitmap"/>.
        /// </summary>
        /// <param name="pixels">An jagged array containing the pixel values
        /// as double arrays. Each element of the arrays will be converted to
        /// a R, G, B, A value. The bits per pixel of the resulting image
        /// will be set according to the size of these arrays.</param>
        /// <param name="width">The width of the resulting image.</param>
        /// <param name="height">The height of the resulting image.</param>
        /// <returns>A <see cref="System.Drawing.Bitmap"/> of given width and height
        /// containing the given pixel values.</returns>
        public static Bitmap ToBitmap(this double[][] pixels, int width, int height)
        {
            return ToBitmap(pixels, width, height, -1, 1);
        }
        #endregion


        #region Framework-wide random number generator

        /// <summary>
        ///   Gets a reference to the random number generator used
        ///   internally by the Accord.NET classes and methods.
        /// </summary>
        public static Random Random { get { return _random; } }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void SetRandomSeed(int seed)
        {
#if DEBUG
            _random = new Random(seed);
#endif
        }
        #endregion

        #region Math
        public static int[] GetRandom(int n, int k)
        {
            int[] idx = GetRandom(n);
            return idx.Submatrix(k);
        }

        /// <summary>
        ///   Returns a random permutation of size n.
        /// </summary>
        public static int[] GetRandom(int n)
        {
            double[] x = new double[n];
            int[] idx = Matrix.Indexes(0, n);

            for (int i = 0; i < n; i++)
                x[i] = Random.NextDouble();

            Array.Sort(x, idx);

            return idx;
        }


        /// <summary>
        ///   Returns the next power of 2 after the input value x.
        /// </summary>
        /// <param name="x">Input value x.</param>
        /// <returns>Returns the next power of 2 after the input value x.</returns>
        public static int NextPowerOf2(int x)
        {
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return ++x;
        }

        /// <summary>
        ///   Returns the previous power of 2 after the input value x.
        /// </summary>
        /// <param name="x">Input value x.</param>
        /// <returns>Returns the previous power of 2 after the input value x.</returns>
        public static int PreviousPowerOf2(int x)
        {
            return NextPowerOf2(x + 1) / 2;
        }


        /// <summary>
        ///   Hypotenuse calculus without overflow/underflow
        /// </summary>
        /// <param name="a">first value</param>
        /// <param name="b">second value</param>
        /// <returns>The hypotenuse Sqrt(a^2 + b^2)</returns>
        public static double Hypotenuse(double a, double b)
        {
            double r = 0.0;
            double absA = Math.Abs(a);
            double absB = Math.Abs(b);

            if (absA > absB)
            {
                r = b / a;
                r = absA * Math.Sqrt(1 + r * r);
            }
            else if (b != 0)
            {
                r = a / b;
                r = absB * Math.Sqrt(1 + r * r);
            }

            return r;
        }

        /// <summary>
        ///   Gets the proper modulus operation for
        ///   a integer x and modulo m.
        /// </summary>
        public static int Mod(int x, int m)
        {
            if (m < 0) m = -m;
            int r = x % m;
            return r < 0 ? r + m : r;
        }


        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        public static int Scale(this IntRange from, IntRange to, int x)
        {
            if (from.Length == 0) return 0;
            return (to.Length) * (x - from.Min) / from.Length + to.Min;
        }

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        public static double Scale(this DoubleRange from, DoubleRange to, double x)
        {
            if (from.Length == 0) return 0;
            return (to.Length) * (x - from.Min) / from.Length + to.Min;
        }

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        public static double Scale(double fromMin, double fromMax, double toMin, double toMax, double x)
        {
            if (fromMax - fromMin == 0) return 0;
            return (toMax - toMin) * (x - fromMin) / (fromMax - fromMin) + toMin;
        }


        /// <summary>
        ///   Returns the hyperbolic arc cosine of the specified value.
        /// </summary>
        public static double Acosh(double x)
        {
            if (x < 1.0)
                throw new ArgumentOutOfRangeException("x");
            return Math.Log(x + Math.Sqrt(x * x - 1));
        }

        /// <summary>
        /// Returns the hyperbolic arc sine of the specified value.
        /// </summary>
        public static double Asinh(double d)
        {
            double x;
            int sign;

            if (d == 0.0)
                return d;

            if (d < 0.0)
            {
                sign = -1;
                x = -d;
            }
            else
            {
                sign = 1;
                x = d;
            }
            return sign * Math.Log(x + Math.Sqrt(x * x + 1));
        }

        /// <summary>
        /// Returns the hyperbolic arc tangent of the specified value.
        /// </summary>
        public static double Atanh(double d)
        {
            if (d > 1.0 || d < -1.0)
                throw new ArgumentOutOfRangeException("d");
            return 0.5 * Math.Log((1.0 + d) / (1.0 - d));
        }



        /// <summary>
        ///   Returns the factorial falling power of the specified value.
        /// </summary>
        public static int FactorialPower(int value, int degree)
        {
            int t = value;
            for (int i = 0; i < degree; i++)
                t *= degree--;
            return t;
        }

        /// <summary>
        ///   Truncated power function.
        /// </summary>
        public static double TruncatedPower(double value, double degree)
        {
            double x = Math.Pow(value, degree);
            return x > 0 ? x : 0.0;
        }
        #endregion

        public static float Angle(float x, float y)
        {
            if (y >= 0)
            {
                if (x >= 0)
                    return (float)Math.Atan(y / x);
                return (float)(Math.PI - Math.Atan(-y / x));
            }
            else
            {
                if (x >= 0)
                    return (float)(2 * Math.PI - Math.Atan(-y / x));
                return (float)(Math.PI + Math.Atan(y / x));
            }
        }

        public static double Angle(double x, double y)
        {
            if (y >= 0)
            {
                if (x >= 0)
                    return Math.Atan2(y, x);
                return Math.PI - Math.Atan(-y / x);
            }
            else
            {
                if (x >= 0)
                    return 2.0 * Math.PI - Math.Atan2(-y, x);
                return Math.PI + Math.Atan(y / x);
            }
        }
    }

    #region Enums
    /// <summary>
    ///   Directions for the General Comparer.
    /// </summary>
    public enum ComparerDirection
    {
        /// <summary>
        ///   Sorting will be performed in ascending order.
        /// </summary>
        Ascending,
        /// <summary>
        ///   Sorting will be performed in descending order.
        /// </summary>
        Descending
    };

    /// <summary>
    ///   General comparer which supports multiple directions
    ///   and comparison of absolute values.
    /// </summary>
    public class GeneralComparer : IComparer<double>
    {
        private readonly bool _absolute;
        private readonly int _direction = 1;

        /// <summary>
        ///   Constructs a new General Comparer.
        /// </summary>
        /// <param name="direction">The direction to compare.</param>
        public GeneralComparer(ComparerDirection direction)
            : this(direction, false)
        {
        }

        /// <summary>
        ///   Constructs a new General Comparer.
        /// </summary>
        /// <param name="direction">The direction to compare.</param>
        /// <param name="useAbsoluteValues">True to compare absolute values, false otherwise. Default is false.</param>
        public GeneralComparer(ComparerDirection direction, bool useAbsoluteValues)
        {
            _direction = direction == ComparerDirection.Ascending ? 1 : -1;
            _absolute = useAbsoluteValues;
        }

        /// <summary>
        ///   Compares two objects and returns a value indicating whether one is less than,
        ///    equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public int Compare(double x, double y)
        {
            if (_absolute)
                return _direction * Math.Abs(x).CompareTo(Math.Abs(y));
            return _direction * x.CompareTo(y);
        }
    }
    #endregion
}
