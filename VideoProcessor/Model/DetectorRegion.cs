using System;
using System.Drawing;

namespace VideoProcessor.Model
{
    public class DetectorRegion : IComparable<DetectorRegion>
    {
        public DetectorRegion(int x, int y)
        {
            _count = 1;
            _minX = x;
            _minY = y;
            _maxX = x;
            _maxY = y;
        }

        public DetectorRegion(Rectangle rectangle)
        {
            _count = 1000500;
            _minX = rectangle.Left;
            _minY = rectangle.Top;
            _maxX = rectangle.Right;
            _maxY = rectangle.Bottom;
        }

        public bool IsGoodRegion {
            get {
                int width = _maxX - _minX;
                int height = _maxY - _minY;
                return _count > 1 && (width * height > 1000 && width * height < 50000 && width / height < 5 && height / width < 5);
            }
        }

        public bool IsGoodTextRegion {
            get {
                int width = _maxX - _minX;
                int height = _maxY - _minY;
                return _count > 30 && width > 20 && height > 20;
            }
        }

        public float Density { get { return (float)Area / _count; } }

        public int Area {
            get {
                int width = _maxX - _minX;
                int height = _maxY - _minY;
                return width*height;
            }
        }

        private int _count;
        private int _minX;
        private int _minY;
        private int _maxX;
        private int _maxY;

        public Rectangle Rectangle {
            get {
                return new Rectangle(_minX, _minY, _maxX - _minX, _maxY - _minY);
            }
        }

        public bool Check(int x, int y, int minDistance = 40)
        {
            if (_maxX == 0) return true;
            var dx = Math.Max(Math.Max(_minX - x, x - _maxX), 0);
            var dy = Math.Max(Math.Max(_minY - y, y - _maxY), 0);
            return Math.Sqrt(dx*dx + dy*dy) < minDistance;
        }

        public void Add(int x, int y)
        {
            if (x < _minX) _minX = x;
            if (x > _maxX) _maxX = x;
            if (y < _minY) _minY = y;
            if (y > _maxY) _maxY = y;
            _count++;
        }

        public int CompareTo(DetectorRegion other)
        {
            return Area.CompareTo(other.Area);
        }

        public override string ToString()
        {
            return string.Format("DetectorRegion({0}, {1}, {2}, {3}, {4}) => {5}x{6}", _minX, _minY, _maxX, _maxY, _count, _maxX - _minX, _maxY - _minY);
        }
    }
}