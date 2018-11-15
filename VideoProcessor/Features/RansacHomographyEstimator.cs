using System;
using System.Drawing;

using AForge;
using Point = AForge.Point;

namespace VideoProcessor.Features
{
    public class RansacHomographyEstimator
    {
        private readonly Ransac<MatrixH> _ransac;
        private int[] _inliers;

        private PointF[] _pointSet1;
        private PointF[] _pointSet2;


        //Gets the RANSAC estimator used.
        public Ransac<MatrixH> Ransac
        {
            get { return _ransac; }
        }

        //Gets the final set of inliers detected by RANSAC.
        public int[] Inliers
        {
            get { return _inliers; }
        }


        //Creates a new RANSAC homography estimator.
        public RansacHomographyEstimator(double threshold, double probability)
        {
            // Create a new RANSAC with the selected threshold and probability for 4 min
            _ransac = new Ransac<MatrixH>(4, threshold, probability)
            {
                Fitting = Homography,
                Degenerate = Degenerate,
                Distances = Distance
            };
        }

        //Matches two sets of points using RANSAC.
        public MatrixH Estimate(Point[] points1, Point[] points2)
        {
            // Initial argument checkings
            if (points1.Length != points2.Length)
                throw new ArgumentException("The number of points should be equal.");

            if (points1.Length < 4)
                throw new ArgumentException("At least four points are required to fit an homography");

            PointF[] p1 = new PointF[points1.Length];
            PointF[] p2 = new PointF[points2.Length];
            for (int i = 0; i < points1.Length; i++)
            {
                p1[i] = new PointF(points1[i].X, points1[i].Y);
                p2[i] = new PointF(points2[i].X, points2[i].Y);
            }

            return Estimate(p1, p2);
        }

        //Matches two sets of points using RANSAC.
        public MatrixH Estimate(IntPoint[] points1, IntPoint[] points2)
        {
            // Initial argument checkings
            if (points1.Length != points2.Length)
                throw new ArgumentException("The number of points should be equal.");

            if (points1.Length < 4)
                throw new ArgumentException("At least four points are required to fit an homography");

            PointF[] p1 = new PointF[points1.Length];
            PointF[] p2 = new PointF[points2.Length];
            for (int i = 0; i < points1.Length; i++)
            {
                p1[i] = new PointF(points1[i].X, points1[i].Y);
                p2[i] = new PointF(points2[i].X, points2[i].Y);
            }

            return Estimate(p1, p2);
        }

        //Matches two sets of points using RANSAC.
        public MatrixH Estimate(PointF[] points1, PointF[] points2)
        {
            // Initial argument checkings
            if (points1.Length != points2.Length)
                throw new ArgumentException("The number of points should be equal.");

            if (points1.Length < 4)
                throw new ArgumentException("At least four points are required to fit an homography");


            // Normalize each set of points so that the origin is
            //  at centroid and mean distance from origin is sqrt(2).
            MatrixH t1, t2;
            _pointSet1 = points1.Normalize(out t1);
            _pointSet2 = points2.Normalize(out t2);


            // Compute RANSAC and find the inlier points
            MatrixH h = _ransac.Compute(points1.Length, out _inliers);

            if (_inliers == null || _inliers.Length < 4)
                //throw new Exception("RANSAC could not find enough points to fit an homography.");
                return null;


            // Compute the final homography considering all inliers
            h = Homography(_inliers);

            // Denormalise
            h = t2.Inverse() * (h * t1);

            return h;
        }

        //Estimates a homography with the given points.
        private MatrixH Homography(int[] points)
        {
            // Retrieve the original points
            PointF[] x1 = _pointSet1.Submatrix(points);
            PointF[] x2 = _pointSet2.Submatrix(points);

            // Compute the homography
            return Tools.Homography(x1, x2);
        }

        //Compute inliers using the Symmetric Transfer Error,
        private int[] Distance(MatrixH H, double t)
        {
            int n = _pointSet1.Length;

            // Compute the projections (both directions)
            PointF[] p1 = H.TransformPoints(_pointSet1);
            PointF[] p2 = H.Inverse().TransformPoints(_pointSet2);

            // Compute the distances
            double[] d2 = new double[n];
            for (int i = 0; i < n; i++)
            {
                // Compute the distance as
                float ax = _pointSet1[i].X - p2[i].X;
                float ay = _pointSet1[i].Y - p2[i].Y;
                float bx = _pointSet2[i].X - p1[i].X;
                float by = _pointSet2[i].Y - p1[i].Y;
                d2[i] = (ax * ax) + (ay * ay) + (bx * bx) + (by * by);
            }

            // Find and return the inliers
            return d2.Find(z => z < t);
        }

        //Checks if the selected points will result in a degenerate homography.
        private bool Degenerate(int[] points)
        {
            PointF[] x1 = _pointSet1.Submatrix(points);
            PointF[] x2 = _pointSet2.Submatrix(points);

            // If any three of the four points in each set is colinear,
            //  the resulting homography matrix will be degenerate.

            return Tools.Colinear(x1[0], x1[1], x1[2]) ||
                   Tools.Colinear(x1[0], x1[1], x1[3]) ||
                   Tools.Colinear(x1[0], x1[2], x1[3]) ||
                   Tools.Colinear(x1[1], x1[2], x1[3]) ||

                   Tools.Colinear(x2[0], x2[1], x2[2]) ||
                   Tools.Colinear(x2[0], x2[1], x2[3]) ||
                   Tools.Colinear(x2[0], x2[2], x2[3]) ||
                   Tools.Colinear(x2[1], x2[2], x2[3]);
        }
    }
}
