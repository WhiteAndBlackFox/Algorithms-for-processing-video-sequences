using System;
using VideoProcessor.Model;

namespace VideoProcessor.MotionDetector {
    public class BlockMatchingDetector
    {
        private Frame _prevFrame;
        private int _width;
        private int _height;
        private int _stride;

        public DetectorResult Process(Frame frame, int blockSize, int threshold)
        {
            DetectorResult detectorResult = new DetectorResult();
            if (_prevFrame == null)
            {
                _prevFrame = frame;
                _width = frame.BitmapData.Width;
                _height = frame.BitmapData.Height;
                _stride = frame.BitmapData.Stride;
                return detectorResult;
            }

            for (int row = 0; row < _height; row += blockSize)
            {
                for (int col = 0; col < _width; col += blockSize)
                {
                    double err = 0;
                    int k = 0;
                    for (int i = row; i < Math.Min(row + blockSize, _height); i++)
                    {
                        for (int j = col; j < Math.Min(col + blockSize, _width); j++)
                        {
                            var index = i * _stride + j * 3;
                            k++;
                            /*err += Math.Abs(
                                    ColorModel.GetBrightness(frame.Buffer[index + 2], frame.Buffer[index + 1], frame.Buffer[index]) -
                                    ColorModel.GetBrightness(_prevFrame.Buffer[index + 2], _prevFrame.Buffer[index + 1], _prevFrame.Buffer[index])
                                  );*/

                            err += Math.Abs(
                                (frame.Buffer[index + 2] + frame.Buffer[index + 1] + frame.Buffer[index])/3 - 
                                (_prevFrame.Buffer[index + 2] + _prevFrame.Buffer[index + 1] + _prevFrame.Buffer[index])/3
                            );
                        }
                    }
                    err /= k;
                    if (err > threshold) {
                        detectorResult.Add(col + blockSize / 2, row + blockSize / 2);
                    }
                }
            }

            return detectorResult;
        }
    }
}
