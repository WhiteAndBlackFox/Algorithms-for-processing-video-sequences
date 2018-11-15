using VideoProcessor.Decompositions;
using System;

namespace VideoProcessor.Features
{
    /// <summary>
    ///   Static class Norm. Defines a set of extension methods defining norms measures.
    /// </summary>
    /// 
    public static class Norm
    {
        /// <summary>
        ///   Returns the maximum column sum of the given matrix.
        /// </summary>
        public static double Norm1(this double[,] a)
        {
            double[] columnSums = Matrix.Sum(a, 1);
            return Matrix.Max(columnSums);
        }

        /// <summary>
        ///   Returns the maximum singular value of the given matrix.
        /// </summary>
        public static double Norm2(this double[,] a)
        {
            return new SingularValueDecomposition(a, false, false).TwoNorm;
        }

        /// <summary>
        ///   Gets the square root of the sum of squares for all elements in a matrix.
        /// </summary>
        public static double Frobenius(this double[,] a)
        {
            int rows = a.GetLength(0);
            int cols = a.GetLength(1);

            double norm = 0.0;
            for (int j = 0; j < cols; j++)
            {
                for (int i = 0; i < rows; i++)
                {
                    double v = a[i, j];
                    norm += v * v;
                }
            }

            return System.Math.Sqrt(norm);
        }

        /// <summary>
        ///   Gets the Squared Euclidean norm for a vector.
        /// </summary>
        public static double SquareEuclidean(this double[] a)
        {
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i] * a[i];
            return sum;
        }

        /// <summary>
        ///   Gets the Euclidean norm for a vector.
        /// </summary>
        public static double Euclidean(this double[] a)
        {
            return System.Math.Sqrt(SquareEuclidean(a));
        }

        /// <summary>
        ///   Gets the Squared Euclidean norm vector for a matrix.
        /// </summary>
        public static double[] SquareEuclidean(this double[,] a)
        {
            return SquareEuclidean(a, 0);
        }

        /// <summary>
        ///   Gets the Squared Euclidean norm vector for a matrix.
        /// </summary>
        public static double[] SquareEuclidean(this double[,] a, int dimension)
        {
            int rows = a.GetLength(0);
            int cols = a.GetLength(1);
            
            double[] norm;

            if (dimension == 0)
            {
                norm = new double[cols];

                for (int j = 0; j < norm.Length; j++)
                {
                    double sum = 0.0;
                    for (int i = 0; i < rows; i++)
                    {
                        double v = a[i, j];
                        sum += v * v;
                    }
                    norm[j] = sum;
                }
            }
            else
            {
                norm = new double[rows];

                for (int i = 0; i < norm.Length; i++)
                {
                    double sum = 0.0;
                    for (int j = 0; j < cols; j++)
                    {
                        double v = a[i, j];
                        sum += v * v;
                    }
                    norm[i] = sum;
                }
            }

            return norm;
        }

        public static int[] Bottom<T>(this T[] values, int count, bool inPlace = false)
            where T : IComparable
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count",
                "The number of elements to be selected must be positive.");
            }

            if (count == 0)
                return new int[0];

            if (count > values.Length)
                return Matrix.Indices(0, values.Length);

            T[] work = (inPlace) ? values : (T[])values.Clone();

            int[] idx = new int[values.Length];
            for (int i = 0; i < idx.Length; i++)
                idx[i] = i;

            int pivot = select(work, idx, 0, values.Length - 1, count, false);

            int[] result = new int[count];
            for (int i = 0; i < result.Length; i++)
                result[i] = idx[i];

            return result;
        }

        private static int select<T>(T[] list, int[] idx, int left, int right, int k, bool asc)
            where T : IComparable
        {
            while (left != right)
            {
                // select pivotIndex between left and right
                int pivotIndex = (left + right) / 2;

                int pivotNewIndex = partition(list, idx, left, right, pivotIndex, asc);
                int pivotDist = pivotNewIndex - left + 1;

                if (pivotDist == k)
                    return pivotNewIndex;

                else if (k < pivotDist)
                    right = pivotNewIndex - 1;
                else
                {
                    k = k - pivotDist;
                    left = pivotNewIndex + 1;
                }
            }

            return -1;
        }

        private static int partition<T>(T[] list, int[] idx, int left, int right, int pivotIndex, bool asc)
            where T : IComparable
        {
            T pivotValue = list[pivotIndex];

            // Move pivot to end
            swap(ref list[pivotIndex], ref list[right]);
            swap(ref idx[pivotIndex], ref idx[right]);

            int storeIndex = left;

            if (asc)
            {
                for (int i = left; i < right; i++)
                {
                    if (list[i].CompareTo(pivotValue) > 0)
                    {
                        swap(ref list[storeIndex], ref list[i]);
                        swap(ref idx[storeIndex], ref idx[i]);
                        storeIndex++;
                    }
                }
            }
            else
            {
                for (int i = left; i < right; i++)
                {
                    if (list[i].CompareTo(pivotValue) < 0)
                    {
                        swap(ref list[storeIndex], ref list[i]);
                        swap(ref idx[storeIndex], ref idx[i]);
                        storeIndex++;
                    }
                }
            }

            // Move pivot to its final place
            swap(ref list[right], ref list[storeIndex]);
            swap(ref idx[right], ref idx[storeIndex]);
            return storeIndex;
        }

        private static void swap<T>(ref T a, ref T b)
        {
            T aux = a; a = b; b = aux;
        }

        public static double[,] Inverse(this double[,] matrix, bool inPlace)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            if (rows != cols)
                throw new ArgumentException("Matrix must be square", "matrix");

            if (rows == 3)
            {
                // Special case for 3x3 matrices
                double a = matrix[0, 0], b = matrix[0, 1], c = matrix[0, 2];
                double d = matrix[1, 0], e = matrix[1, 1], f = matrix[1, 2];
                double g = matrix[2, 0], h = matrix[2, 1], i = matrix[2, 2];

                double den = a * (e * i - f * h) -
                             b * (d * i - f * g) +
                             c * (d * h - e * g);

                if (den == 0)
                    throw new Exception();

                double m = 1.0 / den;

                double[,] inv = (inPlace) ? matrix : new double[3, 3];
                inv[0, 0] = m * (e * i - f * h);
                inv[0, 1] = m * (c * h - b * i);
                inv[0, 2] = m * (b * f - c * e);
                inv[1, 0] = m * (f * g - d * i);
                inv[1, 1] = m * (a * i - c * g);
                inv[1, 2] = m * (c * d - a * f);
                inv[2, 0] = m * (d * h - e * g);
                inv[2, 1] = m * (b * g - a * h);
                inv[2, 2] = m * (a * e - b * d);

                return inv;
            }

            if (rows == 2)
            {
                // Special case for 2x2 matrices
                double a = matrix[0, 0], b = matrix[0, 1];
                double c = matrix[1, 0], d = matrix[1, 1];

                double den = a * d - b * c;

                if (den == 0)
                    throw new ArgumentException();

                double m = 1.0 / den;

                double[,] inv = (inPlace) ? matrix : new double[2, 2];
                inv[0, 0] = +m * d;
                inv[0, 1] = -m * b;
                inv[1, 0] = -m * c;
                inv[1, 1] = +m * a;

                return inv;
            }

            return new LuDecomposition(matrix, false, inPlace).Inverse();
        }

        /// <summary>
        ///   Gets the Euclidean norm for a matrix.
        /// </summary>
        public static double[] Euclidean(this double[,] a)
        {
            return Euclidean(a, 0);
        }

        /// <summary>
        ///   Gets the Euclidean norm for a matrix.
        /// </summary>
        public static double[] Euclidean(this double[,] a, int dimension)
        {
            double[] norm = Norm.SquareEuclidean(a, dimension);

            for (int i = 0; i < norm.Length; i++)
                norm[i] = System.Math.Sqrt(norm[i]);

            return norm;
        }

    }
}
