using System.Drawing;

namespace VideoProcessor.Features
{
    public struct PointH
    {
        private float _px, _py, _pw;

        public float X
        {
            get { return _px; }
            set { _px = value; }
        }

        public float Y
        {
            get { return _py; }
            set { _py = value; }
        }

        //The inverse scaling factor for X and Y.
        public float W
        {
            get { return _pw; }
            set { _pw = value; }
        }

        public PointH(float x, float y)
        {
            _px = x;
            _py = y;
            _pw = 1;
        }

        public PointH(float x, float y, float w)
        {
            _px = x;
            _py = y;
            _pw = w;
        }

        //Transforms a point using a projection matrix.
        public void Transform(float[,] matrix)
        {
            _px = matrix[0, 0] * _px + matrix[0, 1] * _py + matrix[0, 2] * _pw;
            _py = matrix[1, 0] * _px + matrix[1, 1] * _py + matrix[1, 2] * _pw;
            _pw = matrix[2, 0] * _px + matrix[2, 1] * _py + matrix[2, 2] * _pw;
        }

        //Normalizes the point to have unit scale.
        public void Normalize()
        {
            _px = _px / _pw;
            _py = _py / _pw;
            _pw = 1;
        }

        //Gets whether this point is normalized (w = 1).
        public bool IsNormalized
        {
            get { return _pw == 1f; }
        }

        //Gets whether this point is at infinity (w = 0).
        public bool IsAtInfinity
        {
            get { return _pw == 0f; }
        }

        //Gets whether this point is at the origin.
        public bool IsEmpty
        {
            get { return _px == 0 && _py == 0; }
        }

        //Converts the point to a array representation.
        public double[] ToArray()
        {
            return new double[] { _px, _py, _pw };
        }

        //Multiplication by scalar.
        public static PointH operator *(PointH a, float b)
        {
            return new PointH(b * a.X, b * a.Y, b * a.W);
        }

        //Multiplication by scalar.
        public static PointH operator *(float b, PointH a)
        {
            return a * b;
        }

        //Subtraction.
        public static PointH operator -(PointH a, PointH b)
        {
            return new PointH(a.X - b.X, a.Y - b.Y, a.W - b.W);
        }

        //Addition.
        public static PointH operator +(PointH a, PointH b)
        {
            return new PointH(a.X + b.X, a.Y + b.Y, a.W + b.W);
        }

        //Equality
        public static bool operator ==(PointH a, PointH b)
        {
            return (a._px / a._pw == b._px / b._pw && a._py / a._pw == b._py / b._pw);
        }

        //Inequality
        public static bool operator !=(PointH a, PointH b)
        {
            return (a._px / a._pw != b._px / b._pw || a._py / a._pw != b._py / b._pw);
        }

        //PointF Conversion
        public static implicit operator PointF(PointH a)
        {
            return new PointF((float)(a._px / a._pw), (float)(a._py / a._pw));
        }

        //Converts to a Integer point by computing the ceiling of the point coordinates. 
        public static Point Ceiling(PointH point)
        {
            return new Point(
                (int)System.Math.Ceiling(point._px / point._pw),
                (int)System.Math.Ceiling(point._py / point._pw));
        }

        //Converts to a Integer point by rounding the point coordinates. 
        public static Point Round(PointH point)
        {
            return new Point(
                (int)System.Math.Round(point._px / point._pw),
                (int)System.Math.Round(point._py / point._pw));
        }

        //Converts to a Integer point by truncating the point coordinates. 
        public static Point Truncate(PointH point)
        {
            return new Point(
                (int)System.Math.Truncate(point._px / point._pw),
                (int)System.Math.Truncate(point._py / point._pw));
        }

        //Compares two objects for equality.
        public override bool Equals(object obj)
        {
            if (obj is PointH)
            {
                PointH p = (PointH)obj;
                if (_px / _pw == p._px / p._pw && _py / _pw == p._py / p._pw)
                {
                    return true;
                }
            }

            return false;
        }

        //Returns the hash code for this instance.
        public override int GetHashCode()
        {
            return _px.GetHashCode() ^ _py.GetHashCode() ^ _pw.GetHashCode();
        }

        //Returns the empty point.
        public static readonly PointH Empty = new PointH(0, 0, 1);
    }
}
