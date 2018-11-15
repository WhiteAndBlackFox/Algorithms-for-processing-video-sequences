using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using VideoProcessor.Algorithms;

namespace VideoProcessor.Model {
    public class Frame: IDisposable
    {
        private readonly Bitmap _bitmap;
        private IntPtr _ptr;
        private byte[] _buffer;
        private byte[] _originalBuffer;
        private double _brightness;
        private BitmapData _bitmapData;
        private int _height = -1, _width = -1, _stride = -1;

        public Frame(Image image, bool readOnly = true) {
            _bitmap = new Bitmap((Bitmap)image.Clone(), image.Width, image.Height);
            InitData();
            if (readOnly)
            {
                SaveChanges();
            }
            _brightness = -1;
        }

        public Frame Copy(bool readOnly = false)
        {
            return new Frame(_bitmap, readOnly);
        }

        public Bitmap ToGrayscale() {
            var result = new Bitmap(_bitmap.Width, _bitmap.Height, PixelFormat.Format8bppIndexed);

            BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            byte[] bytes = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (int y = 0; y < data.Height; y++) {
                for (int x = 0; x < data.Width; x++) {
                    var pixelIndex = y * _bitmapData.Stride + x;
                    bytes[y * data.Stride + x] = (byte)((_buffer[pixelIndex + 2] + _buffer[pixelIndex + 1] + _buffer[pixelIndex]) / 3);
                }
            }

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);

            result.UnlockBits(data);

            return result;
        }

        public Bitmap Image
        {
            get { return _bitmap; }
        }

        public IntPtr Ptr { get { return _ptr; } }
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public int Stride { get { return _stride; } }

        public byte[] OriginalBuffer
        {
            get
            {
                if (_originalBuffer == null)
                {
                    InitData();
                }
                return _originalBuffer;
            }
        }

        public byte[] Buffer
        {
            get
            {
                if (_buffer == null)
                {
                    InitData();
                }
                return _buffer;
            }
        }

        public BitmapData BitmapData {
            get {
                if (_bitmapData == null)
                {
                    InitData();
                }
                return _bitmapData;
            }
        }

        public void SaveChanges()
        {
            if (_bitmapData == null) return;
            //Копируем обратно
            Marshal.Copy(_buffer, 0, _ptr, _buffer.Length);
            //Разблокируем байты изображения
            _bitmap.UnlockBits(_bitmapData);
        }

        public bool IsProccessed
        {
            get { return _buffer != null; }
        }

        public void Dispose()
        {
            _bitmap.Dispose();
        }

        public void InitData()
        {
            Rectangle rect = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);
            //Блокируем байты изображения на время манупуляций
            _bitmapData = _bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            //Указатель на пиксельное содержимое изображения
            _ptr = _bitmapData.Scan0;
            
            int length = _bitmapData.Stride * _bitmapData.Height;
            _buffer = new byte[length];
            //Копируем данные изображения в байтовый массив
            Marshal.Copy(_ptr, _buffer, 0, length);
            _originalBuffer = (byte[]) _buffer.Clone();
            _width = _bitmapData.Width;
            _height = _bitmapData.Height;
            _stride = _bitmapData.Stride;
        }

        public double GetAverageBrightness() {
            if (_brightness < 0)
            {
                var bitmapData = BitmapData;
                var buffer = Buffer;
                double brightness = 0;
                for (int row = 0; row < bitmapData.Height; row++) {
                    int byteIndex = row * bitmapData.Stride;
                    for (int col = 0; col < bitmapData.Width; col++) {
                        int pixelIndex = byteIndex + col * 3;
                        brightness += ColorModel.GetBrightness(buffer[pixelIndex + 2], buffer[pixelIndex + 1], buffer[pixelIndex]);
                    }
                }

                _brightness= brightness / (bitmapData.Width * bitmapData.Height);    
            }
            return _brightness;
        }
    }
}
