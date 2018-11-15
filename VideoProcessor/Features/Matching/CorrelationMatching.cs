using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge;
using AForge.Imaging.Filters;

namespace VideoProcessor.Features.Matching
{
    public class CorrelationMatching
    {
        private int _windowSize;
        private double _dmax;

        //Gets or sets the maximum distance to consider points as correlated.
        public double DistanceMax
        {
            get { return _dmax; }
            set { _dmax = value; }
        }

        public int WindowSize
        {
            get { return _windowSize; }
            set { _windowSize = value; }
        }

        public CorrelationMatching(int windowSize)
            : this(windowSize, 0)
        {
        }

        public CorrelationMatching(int windowSize, double maxDistance)
        {
            _windowSize = windowSize;
            _dmax = maxDistance;
        }

        public IntPoint[][] Match(Bitmap image1, Bitmap image2, IntPoint[] points1, IntPoint[] points2)
        {
            // Make sure we are dealing with grayscale images.
            var grayImage1 = image1.PixelFormat == PixelFormat.Format8bppIndexed ? image1 : Grayscale.CommonAlgorithms.BT709.Apply(image1);
            var grayImage2 = image2.PixelFormat == PixelFormat.Format8bppIndexed ? image2 : Grayscale.CommonAlgorithms.BT709.Apply(image2);

            // Generate correlation matrix
            double[,] correlationMatrix = ComputeCorrelationMatrix(grayImage1, points1, grayImage2, points2, _windowSize, _dmax);

            // Free allocated resources
            if (image1.PixelFormat != PixelFormat.Format8bppIndexed)
                grayImage1.Dispose();

            if (image2.PixelFormat != PixelFormat.Format8bppIndexed)
                grayImage2.Dispose();

            // Select points with maximum correlation measures
            int[] colp2Forp1; 
            Matrix.Max(correlationMatrix, 1, out colp2Forp1);
            int[] rowp1Forp2; 
            Matrix.Max(correlationMatrix, 0, out rowp1Forp2);

            // Construct the lists of matched point indices
            int rows = correlationMatrix.GetLength(0);
            var p1Ind = new List<int>();
            var p2Ind = new List<int>();

            // For each point in the first set of points,
            for (int i = 0; i < rows; i++)
            {
                // Get the point j in the second set of points with which
                // this point i has a maximum correlation measure. (i->j)
                int j = colp2Forp1[i];

                // Now, check if this point j in the second set also has
                // a maximum correlation measure with the point i. (j->i)
                if (rowp1Forp2[j] == i)
                {
                    // The points are consistent. Ensure they are valid.
                    if (correlationMatrix[i, j] != Double.NegativeInfinity)
                    {
                        // We have a corresponding pair (i,j)
                        p1Ind.Add(i); 
                        p2Ind.Add(j);
                    }
                }
            }

            // Extract matched points from original arrays
            var m1 = points1.Submatrix(p1Ind.ToArray());
            var m2 = points2.Submatrix(p2Ind.ToArray());

            // Create matching point pairs
            return new[] { m1, m2 };
        }

        //Constructs the correlation matrix between selected points from two images.
        private static double[,] ComputeCorrelationMatrix(Bitmap image1, IntPoint[] points1, Bitmap image2, IntPoint[] points2, int windowSize, double maxDistance)
        {
            // Create the initial correlation matrix
            double[,] matrix = Matrix.Create(points1.Length, points2.Length, Double.NegativeInfinity);

            // Gather some information
            int width1 = image1.Width;
            int width2 = image2.Width;
            int height1 = image1.Height;
            int height2 = image2.Height;

            int r = (windowSize - 1) / 2; //  'radius' of correlation window
            double m = maxDistance * maxDistance; // maximum considered distance
            double[,] w1 = new double[windowSize, windowSize]; // first window
            double[,] w2 = new double[windowSize, windowSize]; // second window

            // Lock the images
            BitmapData bitmapData1 = image1.LockBits(new Rectangle(0, 0, width1, height1), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData bitmapData2 = image2.LockBits(new Rectangle(0, 0, width2, height2), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            int stride1 = bitmapData1.Stride;
            int stride2 = bitmapData2.Stride;

            // We will ignore points at the edge
            int[] idx1 = points1.Find(p => p.X >= r && p.X < width1 - r && p.Y >= r && p.Y < height1 - r);
            int[] idx2 = points2.Find(p => p.X >= r && p.X < width2 - r && p.Y >= r && p.Y < height2 - r);

            // For each index in the first set of points
            foreach (int n1 in idx1)
            {
                // Get the current point
                var p1 = points1[n1];

                unsafe // Create the first window for the current point
                {
                    byte* src = (byte*)bitmapData1.Scan0 + (p1.X - r) + (p1.Y - r) * stride1;

                    for (int j = 0; j < windowSize; j++)
                    {
                        for (int i = 0; i < windowSize; i++)
                        {
                            w1[i, j] = *(src + i);
                        }
                        src += stride1;
                    }
                }

                // Normalize the window
                double sum = 0;
                for (int i = 0; i < windowSize; i++)
                    for (int j = 0; j < windowSize; j++)
                        sum += w1[i, j] * w1[i, j];
                sum = Math.Sqrt(sum);
                for (int i = 0; i < windowSize; i++)
                    for (int j = 0; j < windowSize; j++)
                        w1[i, j] /= sum;


                // Identify the indices of points in p2 that we need to consider.
                int[] candidates;
                if (maxDistance == 0)
                {
                    // We should consider all points
                    candidates = idx2;
                }
                else
                {
                    // We should consider points that are within
                    //  distance maxDistance apart

                    // Compute distances from the current point
                    //  to all points in the second image.
                    double[] distances = new double[idx2.Length];
                    for (int i = 0; i < idx2.Length; i++)
                    {
                        double dx = p1.X - points2[idx2[i]].X;
                        double dy = p1.Y - points2[idx2[i]].Y;
                        distances[i] = dx * dx + dy * dy;
                    }

                    candidates = idx2.Submatrix(distances.Find(d => d < m));
                }

                // Calculate normalized correlation measure
                foreach (int n2 in candidates)
                {
                    var p2 = points2[n2];

                    unsafe // Generate window in 2nd image
                    {
                        byte* src = (byte*)bitmapData2.Scan0 + (p2.X - r) + (p2.Y - r) * stride2;

                        for (int j = 0; j < windowSize; j++)
                        {
                            for (int i = 0; i < windowSize; i++)
                                w2[i, j] = *(src + i);
                            src += stride2;
                        }
                    }

                    double sum1 = 0, sum2 = 0;
                    for (int i = 0; i < windowSize; i++)
                    {
                        for (int j = 0; j < windowSize; j++)
                        {
                            sum1 += w1[i, j] * w2[i, j];
                            sum2 += w2[i, j] * w2[i, j];
                        }
                    }

                    matrix[n1, n2] = sum1 / Math.Sqrt(sum2);
                }
            }

            image1.UnlockBits(bitmapData1);
            image2.UnlockBits(bitmapData2);

            return matrix;
        }
    }
}
