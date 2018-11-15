using System;
using System.Drawing;


namespace VideoProcessor.Features
{
    [Serializable]
    public class MatrixH
    {

        public float[] Elements { get; set; }

        /// <summary>
        ///   Creates a new projective matrix.
        /// </summary>
        public MatrixH()
        {
            // Start as the identity matrix
            Elements = new float[] { 1, 0, 0, 0, 1, 0, 0, 0 };
        }

        /// <summary>
        ///   Creates a new projective matrix.
        /// </summary>
        public MatrixH(float m11, float m12, float m13,
                       float m21, float m22, float m23,
                       float m31, float m32)
        {
            Elements = new float[8];
            Elements[0] = m11; Elements[1] = m12; Elements[2] = m13;
            Elements[3] = m21; Elements[4] = m22; Elements[5] = m23;
            Elements[6] = m31; Elements[7] = m32;
        }

        //Creates a new projective matrix.
        public MatrixH(float m11, float m12, float m13,
                       float m21, float m22, float m23,
                       float m31, float m32, float m33)
            : this(m11, m12, m13, m21, m22, m23, m31, m32)
        {
            for (int i = 0; i < 8; i++)
                Elements[i] /= m33;
        }

        //Creates a new projective matrix.
        public MatrixH(double[,] H)
        {
            Elements = new float[8];
            for (int i = 0, k = 0; i < 3; i++)
                for (int j = 0; j < 3 && k < 8; j++, k++)
                    Elements[k] = (float)(H[i, j] / H[2, 2]);
        }

        public float OffsetX
        {
            get { return Elements[2]; }
        }

        public float OffsetY
        {
            get { return Elements[5]; }
        }

        //Gets whether this matrix is invertible.
        public bool IsInvertible
        {
            get
            {
                float det = Elements[0] * (Elements[4] - Elements[5] * Elements[7])
                    - Elements[1] * (Elements[3] - Elements[5] * Elements[6])
                    + Elements[2] * (Elements[3] * Elements[7] - Elements[4] * Elements[6]);
                return det > 0;
            }
        }

        //Gets whether this is an Affine transformation matrix.
        public bool IsAffine
        {
            get { return (Elements[6] == 0 && Elements[7] == 0); }
        }

        //Gets whether this is the identity transformation.
        public bool IsIdentity
        {
            get
            {
                return
                    Elements[0] == 1 && Elements[1] == 0 && Elements[2] == 0 &&
                    Elements[3] == 0 && Elements[4] == 1 && Elements[5] == 0 &&
                    Elements[6] == 0 && Elements[7] == 0;
            }
        }

        //Resets this matrix to be the identity.
        public void Reset()
        {
            Elements[0] = 1; Elements[1] = 0; Elements[2] = 0;
            Elements[3] = 0; Elements[4] = 1; Elements[5] = 0;
            Elements[6] = 0; Elements[7] = 0;
        }

        //Returns the inverse matrix, if this matrix is invertible.
        public MatrixH Inverse()
        {
            //    m = 1 / [a(ei-fh) - b(di-fg) + c(dh-eg)]
            // 
            //                  (ei-fh)   (ch-bi)   (bf-ce)
            //  inv(A) =  m  x  (fg-di)   (ai-cg)   (cd-af)
            //                  (dh-eg)   (bg-ah)   (ae-bd)
            //

            float a = Elements[0], b = Elements[1], c = Elements[2];
            float d = Elements[3], e = Elements[4], f = Elements[5];
            float g = Elements[6], h = Elements[7];

            float m = 1f / (a * (e - f * h) - b * (d - f * g) + c * (d * h - e * g));
            float na = m * (e - f * h);
            float nb = m * (c * h - b);
            float nc = m * (b * f - c * e);
            float nd = m * (f * g - d);
            float ne = m * (a - c * g);
            float nf = m * (c * d - a * f);
            float ng = m * (d * h - e * g);
            float nh = m * (b * g - a * h);
            float nj = m * (a * e - b * d);

            return new MatrixH(na, nb, nc, nd, ne, nf, ng, nh, nj);
        }

        //Transforms the given points using this transformation matrix.
        public PointH[] TransformPoints(params PointH[] points)
        {
            PointH[] r = new PointH[points.Length];

            for (int j = 0; j < points.Length; j++)
            {
                r[j].X = Elements[0] * points[j].X + Elements[1] * points[j].Y + Elements[2] * points[j].W;
                r[j].Y = Elements[3] * points[j].X + Elements[4] * points[j].Y + Elements[5] * points[j].W;
                r[j].W = Elements[6] * points[j].X + Elements[7] * points[j].Y + points[j].W;
            }

            return r;
        }

        //Transforms the given points using this transformation matrix.
        public PointF[] TransformPoints(params PointF[] points)
        {
            PointF[] r = new PointF[points.Length];

            for (int j = 0; j < points.Length; j++)
            {
                float w = Elements[6] * points[j].X + Elements[7] * points[j].Y + 1f;
                r[j].X = (Elements[0] * points[j].X + Elements[1] * points[j].Y + Elements[2]) / w;
                r[j].Y = (Elements[3] * points[j].X + Elements[4] * points[j].Y + Elements[5]) / w;
            }

            return r;
        }

        //Multiplies this matrix, returning a new matrix as result.
        public MatrixH Multiply(MatrixH matrix)
        {
            float na = Elements[0] * matrix.Elements[0] + Elements[1] * matrix.Elements[3] + Elements[2] * matrix.Elements[6];
            float nb = Elements[0] * matrix.Elements[1] + Elements[1] * matrix.Elements[4] + Elements[2] * matrix.Elements[7];
            float nc = Elements[0] * matrix.Elements[2] + Elements[1] * matrix.Elements[5] + Elements[2];

            float nd = Elements[3] * matrix.Elements[0] + Elements[4] * matrix.Elements[3] + Elements[5] * matrix.Elements[6];
            float ne = Elements[3] * matrix.Elements[1] + Elements[4] * matrix.Elements[4] + Elements[5] * matrix.Elements[7];
            float nf = Elements[3] * matrix.Elements[2] + Elements[4] * matrix.Elements[5] + Elements[5];

            float ng = Elements[6] * matrix.Elements[0] + Elements[7] * matrix.Elements[3] + matrix.Elements[6];
            float nh = Elements[6] * matrix.Elements[1] + Elements[7] * matrix.Elements[4] + matrix.Elements[7];
            float ni = Elements[6] * matrix.Elements[2] + Elements[7] * matrix.Elements[5] + 1f;

            return new MatrixH(na, nb, nc, nd, ne, nf, ng, nh, ni);
        }

        //Compares two objects for equality.
        public override bool Equals(object obj)
        {
            if (obj is MatrixH)
            {
                MatrixH m = obj as MatrixH;
                return this == m;
            }
            return false;
        }

        //Returns the hash code for this instance.
        public override int GetHashCode()
        {
            return Elements.GetHashCode();
        }

        //Double[,] conversion.
        public static explicit operator double[,](MatrixH matrix)
        {
            return new[,] 
            {
                { matrix.Elements[0], matrix.Elements[1], matrix.Elements[2] },
                { matrix.Elements[3], matrix.Elements[4], matrix.Elements[5] },
                { matrix.Elements[6], matrix.Elements[7], 1.0 },
            };
        }

        //Single[,] conversion.
        public static explicit operator float[,](MatrixH matrix)
        {
            return new [,] 
            {
                { matrix.Elements[0], matrix.Elements[1], matrix.Elements[2] },
                { matrix.Elements[3], matrix.Elements[4], matrix.Elements[5] },
                { matrix.Elements[6], matrix.Elements[7], 1.0f },
            };
        }

        //Matrix multiplication.
        public static MatrixH operator *(MatrixH matrix1, MatrixH matrix2)
        {
            return matrix1.Multiply(matrix2);
        }

        //Equality
        public static bool operator ==(MatrixH a, MatrixH b)
        {
            for (int i = 0; i < 8; i++)
                if (a.Elements[i] != b.Elements[i])
                    return false;

            return true;
        }

        //Inequality
        public static bool operator !=(MatrixH a, MatrixH b)
        {
            for (int i = 0; i < 8; i++)
                if (a.Elements[i] == b.Elements[i])
                    return true;

            return false;
        }

    }
}
