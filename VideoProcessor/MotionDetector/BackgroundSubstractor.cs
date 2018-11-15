using System;
using System.Drawing.Imaging;
using System.Threading;
using VideoProcessor.Algorithms;
using VideoProcessor.Model;

namespace VideoProcessor.MotionDetector {
    public class BackgroundSubstractor
    {
        //private const int StartBackgroundProcessing = 50;
        private readonly byte[][] _buffers = new byte[150][];
        private byte[] _background;
        private int _processNumber;

        public DetectorResult Process(Frame frame, int threshold)
        {
            DetectorResult detectorResult  = new DetectorResult();
            if (_processNumber == _buffers.Length/* || _background == null && _processNumber == StartBackgroundProcessing*/)
            {
                //if (_processNumber == StartBackgroundProcessing)
                //{
                //    _buffers[_processNumber] = frame.Buffer;
                //}

                var background = frame.Copy();
                new Thread(data =>
                {
                    int processNumber = (int) data;
                    BitmapData bitmapData = background.BitmapData;
                    byte[] buffer = background.Buffer;
                    for (int row = 0; row < bitmapData.Height; row++) {
                        int byteIndex = row * bitmapData.Stride;
                        for (int col = 0; col < bitmapData.Width; col++) {
                            int pixelIndex = byteIndex + col * 3;
                            double sumR = 0, sumG = 0, sumB = 0;
                            for (int index = 0; index < processNumber; index++) {
                                sumR += _buffers[index][pixelIndex + 2];
                                sumG += _buffers[index][pixelIndex + 1];
                                sumB += _buffers[index][pixelIndex];
                            }
                            buffer[pixelIndex + 2] = GetAv(sumR, processNumber);
                            buffer[pixelIndex + 1] = GetAv(sumG, processNumber);
                            buffer[pixelIndex] = GetAv(sumB, processNumber);
                        }
                    }

                    background.SaveChanges();
                    _background = background.Buffer;
                    background.Image.Save("./"+background.GetHashCode()+".jpg", ImageFormat.Jpeg);
                }).Start(_processNumber);

                if (_processNumber == _buffers.Length)
                {
                    _processNumber = 0;
                }
                else
                {
                    _processNumber++;
                }
            }
            else if (_processNumber < _buffers.Length)
            {
                _buffers[_processNumber] = frame.Buffer;
                _processNumber++;
            }

            if (_background != null)
            {
                BitmapData bitmapData = frame.BitmapData;
                byte[] buffer = frame.Buffer;
                for (int row = 0; row < bitmapData.Height; row++) {
                    int byteIndex = row * bitmapData.Stride;
                    for (int col = 0; col < bitmapData.Width; col++) {
                        int pixelIndex = byteIndex + col * 3;

                        if (Math.Abs(
                            ColorModel.GetBrightness(buffer[pixelIndex + 2], buffer[pixelIndex + 1], buffer[pixelIndex])
                            -
                            ColorModel.GetBrightness(_background[pixelIndex + 2], _background[pixelIndex + 1], _background[pixelIndex])) > threshold)
                        {
                            detectorResult.Add(col, row);
                        }
                    }
                }
            }

            return detectorResult;
        }

        private byte GetAv(double sum, double k) {
            sum /= k;
            if (sum < 0) sum = 0;
            else if (sum > 255) sum = 255;
            return (byte)sum;
        }
    }
}
