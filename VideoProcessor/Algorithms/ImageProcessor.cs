using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using AForge;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VideoProcessor.Model;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Point = System.Drawing.Point;

namespace VideoProcessor.Algorithms
{
    public class ImageProcessor
    {
        #region Constructor & private fields

        protected readonly Random Random;

        private readonly int[][] _sobelMatrixGx1 = 
        {
            new [] {1, 0, -1},
            new [] {2, 0, -2},
            new [] {1, 0, -1}
        };

        private readonly int[][] _sobelMatrixGy1 = 
        {
            new [] {1, 2, 1},
            new [] {0, 0, 0},
            new [] {-1, -2, -1}
        };

        public ImageProcessor()
        {
            Random = new Random(new DateTime().Millisecond);
            Parallel.ThreadsCount = 5;
        }

        #endregion

        #region Gamma Correction

        public void AlgorithmGammaCorrection(Frame frame, double value)
        {
            var bitmapData = frame.BitmapData;
            var buffer = frame.Buffer;
            var rampTable = new byte[256];
            for (int i = 0; i < rampTable.Length; i++) {
                rampTable[i] = (byte)(Math.Pow((float)i/255, value)*255.0f);
            }
            for (int row = 0; row < bitmapData.Height; row++) {
                int byteIndex = row * bitmapData.Stride;
                for (int col = 0; col < bitmapData.Width; col++) {
                    int pixelIndex = byteIndex + col * 3;
                    double y, cb, cr;
                    ColorModel.ToYCbCr(buffer[pixelIndex + 2], buffer[pixelIndex + 1], buffer[pixelIndex], out y, out cb, out cr);
                    ColorModel.FromYCbCr(rampTable[(int)y], cb, cr, out buffer[pixelIndex + 2], out buffer[pixelIndex + 1], out buffer[pixelIndex]);
                }
            }

            /*
            var buffer = frame.Buffer;
            var bitmapData = frame.BitmapData;
            for (int row = 0; row < bitmapData.Height; row++)
            {
                int byteIndex = row * bitmapData.Stride;
                for (int col = 0; col < bitmapData.Width; col++)
                {
                    int pixelIndex = byteIndex + col * 3;
                    buffer[pixelIndex + 2] = rampTable[buffer[pixelIndex + 2]];
                    buffer[pixelIndex + 1] = rampTable[buffer[pixelIndex + 1]];
                    buffer[pixelIndex] = rampTable[buffer[pixelIndex]];
                }
            }*/
        }
        #endregion
        
        #region Log Correction

        public void AlgorithmLogCorrection(Frame frame, int k)
        {
            var bitmapData = frame.BitmapData;
            var buffer = frame.Buffer;
            for (int row = 0; row < bitmapData.Height; row++)
            {
                int byteIndex = row * bitmapData.Stride;
                for (int col = 0; col < bitmapData.Width; col++)
                {
                    int pixelIndex = byteIndex + col * 3;
                    double y, cb, cr;
                    ColorModel.ToYCbCr(buffer[pixelIndex + 2], buffer[pixelIndex + 1], buffer[pixelIndex], out y, out cb, out cr);
                    ColorModel.FromYCbCr(k * Math.Log(1 + y), cb, cr, out buffer[pixelIndex + 2], out buffer[pixelIndex + 1], out buffer[pixelIndex]);
                }
            }
        }

        #endregion

        #region Autolevel
        public void AlgorithmAutoLevel(Frame frame) {
            var buffer = frame.Buffer;
            var bitmapData = frame.BitmapData;
            float minR = 0, minG = 0, minB = 0, maxR = 0, maxG = 0, maxB = 0;

            for (int row = 0; row < bitmapData.Height; row++)
            {
                int byteIndex = row * bitmapData.Stride;
                for (int col = 0; col < bitmapData.Width; col++)
                {
                    int pixelIndex = byteIndex + col * 3;
                    if ((float)buffer[pixelIndex + 2] > maxR)
                        maxR = buffer[pixelIndex + 2];
                    if ((float)buffer[pixelIndex + 1] > maxG)
                        maxG = buffer[pixelIndex + 1];
                    if ((float)buffer[pixelIndex] > maxB)
                        maxB = buffer[pixelIndex];
                    if ((float)buffer[pixelIndex + 2] < minR)
                        minR = buffer[pixelIndex + 2];
                    if ((float)buffer[pixelIndex + 1] < minG)
                        minG = buffer[pixelIndex + 1];
                    if ((float)buffer[pixelIndex] < minB)
                        minB = buffer[pixelIndex];
                }
            }
            for (int row = 0; row < bitmapData.Height; row++)
            {
                int byteIndex = row * bitmapData.Stride;
                for (int col = 0; col < bitmapData.Width; col++)
                {
                    int pixelIndex = byteIndex + col * 3;
                    buffer[pixelIndex + 2] = (byte)(((float)buffer[pixelIndex + 2] - minR) * 255 / (maxR - minR));
                    buffer[pixelIndex + 1] = (byte)(((float)buffer[pixelIndex + 1] - minG) * 255 / (maxG - minG));
                    buffer[pixelIndex] = (byte)(((float)buffer[pixelIndex] - minB) * 255 / (maxB - minB));
                }
            }
        }
        #endregion

        #region Histogram
        public void AlgorithmHistogram(Frame frame, int minR, int minG, int minB, int maxR, int maxG, int maxB) {
            var buffer = frame.Buffer;
            var bitmapData = frame.BitmapData;
            for (int row = 0; row < bitmapData.Height; row++)
            {
                int byteIndex = row * bitmapData.Stride;
                for (int col = 0; col < bitmapData.Width; col++)
                {
                    int pixelIndex = byteIndex + col * 3;

                    if ((int)buffer[pixelIndex + 2] < minR || (int)buffer[pixelIndex + 2] > maxR) { buffer[pixelIndex + 2] = (byte)0; }
                    else { buffer[pixelIndex + 2] = (byte)255; }

                    if ((int)buffer[pixelIndex + 1] < minG || (int)buffer[pixelIndex + 1] > maxG) { buffer[pixelIndex + 1] = (byte)0; }
                    else { buffer[pixelIndex + 1] = (byte)255; }

                    if ((int)buffer[pixelIndex] < minB || (int)buffer[pixelIndex] > maxB) { buffer[pixelIndex] = (byte)0; }
                    else { buffer[pixelIndex] = (byte)255; }
                }
            }
        }
        #endregion

        #region GrayScale
        public void AlgorithmGrayScale(Frame frame, Func<byte, byte, byte, byte> grayScaleType) {
            var buffer = frame.Buffer;
            var bitmapData = frame.BitmapData;
            for (int row = 0; row < bitmapData.Height; row++) {
                int byteIndex = row * bitmapData.Stride;
                for (int col = 0; col < bitmapData.Width; col++) {
                    int pixelIndex = byteIndex + col * 3;

                    byte grayColor = grayScaleType(buffer[pixelIndex + 2], buffer[pixelIndex + 1],
                        buffer[pixelIndex]);
                    buffer[pixelIndex + 2] = buffer[pixelIndex + 1] = buffer[pixelIndex] = grayColor;
                }
            }
        }

        #endregion

        #region EffectsForm

        #region LinearAverage

        public void FilterLinearAverage(Frame frame, Frame[] frames, int frameNumber, int radius, ColorModelEnum colorModel, ProcessTypeEnum processType)
        {
            Console.Write("I'm HERE!");
            if (colorModel == ColorModelEnum.Rgb)
            {
                FilterLinearAverageRgb(frame, frames, frameNumber, radius, processType);
            }
            else
            {
                FilterLinearAverageRgb(frame, frames, frameNumber, radius, processType);
            }
        }

        public void FilterLinearAverageRgb(Frame frame, Frame[] frames, int frameNumber, int radius, ProcessTypeEnum processType)
        {
            var bitmapData = frame.BitmapData;
            var result = frame.Buffer;
            var buffers = frames.Select(item => item.Buffer).ToArray();
            if (processType == ProcessTypeEnum.Parallelepiped)
            {
                Parallel.For(0, bitmapData.Height, row => {
                    int byteIndex = row * bitmapData.Stride;
                    for (int col = 0; col < bitmapData.Width; col++) {
                        int pixelIndex = byteIndex + col * 3;
                        var n = 0;
                        double r1 = 0, g1 = 0, b1 = 0;
                        for (int fi = 0; fi < frameNumber; fi++) {
                            for (int i = -radius; i <= radius; i++) {
                                var row2 = ColorModel.GetValue(row + i, 0, bitmapData.Height - 1) * bitmapData.Stride;
                                for (int j = -radius; j <= radius; j++) {
                                    var col2 = ColorModel.GetValue(col + j, 0, bitmapData.Width - 1) * 3;

                                    r1 += 1f / buffers[fi][row2 + col2 + 2];
                                    g1 += 1f / buffers[fi][row2 + col2 + 1];
                                    b1 += 1f / buffers[fi][row2 + col2];
                                    n++;
                                }
                            }
                        }

                        result[pixelIndex + 2] = (byte)ColorModel.GetValue((int)(n / r1), 0, 255);
                        result[pixelIndex + 1] = (byte)ColorModel.GetValue((int)(n / g1), 0, 255);
                        result[pixelIndex] = (byte)ColorModel.GetValue((int)(n / b1), 0, 255);
                    }
                });
            }
            else
            {
                Parallel.For(0, bitmapData.Height, row => {
                    int byteIndex = row * bitmapData.Stride;
                    for (int col = 0; col < bitmapData.Width; col++) {
                        int pixelIndex = byteIndex + col * 3;

                        var n = 0;
                        double r1 = 0, g1 = 0, b1 = 0;
                        for (int fi = 0; fi < frameNumber; fi++) {
                            var r = radius - radius * (fi) / frameNumber;
                            for (int i = -r; i <= r; i++) {
                                var row2 = ColorModel.GetValue(row + i, 0, bitmapData.Height - 1) * bitmapData.Stride;
                                for (int j = -r; j <= r; j++) {
                                    var col2 = ColorModel.GetValue(col + j, 0, bitmapData.Width - 1) * 3;

                                    r1 += 1f / buffers[fi][row2 + col2 + 2];
                                    g1 += 1f / buffers[fi][row2 + col2 + 1];
                                    b1 += 1f / buffers[fi][row2 + col2];
                                    n++;
                                }
                            }
                        }


                        result[pixelIndex + 2] = (byte)ColorModel.GetValue((int)(n / r1), 0, 255);
                        result[pixelIndex + 1] = (byte)ColorModel.GetValue((int)(n / g1), 0, 255);
                        result[pixelIndex] = (byte)ColorModel.GetValue((int)(n / b1), 0, 255);
                    }
                });
            }
        }
        #endregion

        #region Median
        public void FilterMedian(Frame frame, Frame[] frames, int frameNumber, int radius, ColorModelEnum colorModel, ProcessTypeEnum processType) {
            var bitmapData = frame.BitmapData;
            var result = frame.Buffer;
            var buffers = frames.Select(item => item.Buffer).ToArray();
            if (processType == ProcessTypeEnum.Parallelepiped)
            {
                Parallel.For(0, bitmapData.Height, row => {
                    
                    int byteIndex = row * bitmapData.Stride;
                    for (int col = 0; col < bitmapData.Width; col++) {
                        int pixelIndex = byteIndex + col * 3;
                        var n = 0;
                        double r1 = 0, g1 = 0, b1 = 0;
                        for (int fi = 0; fi < frameNumber; fi++) {
                            for (int i = -radius; i <= radius; i++) {
                                var row2 = ColorModel.GetValue(row + i, 0, bitmapData.Height - 1) * bitmapData.Stride;
                                for (int j = -radius; j <= radius; j++) {
                                    var col2 = ColorModel.GetValue(col + j, 0, bitmapData.Width - 1) * 3;

                                    r1 += 1f / buffers[fi][row2 + col2 + 2];
                                    g1 += 1f / buffers[fi][row2 + col2 + 1];
                                    b1 += 1f / buffers[fi][row2 + col2];
                                    n++;
                                }
                            }
                        }

                        result[pixelIndex + 2] = (byte)ColorModel.GetValue((int)(n / r1), 0, 255);
                        result[pixelIndex + 1] = (byte)ColorModel.GetValue((int)(n / g1), 0, 255);
                        result[pixelIndex] = (byte)ColorModel.GetValue((int)(n / b1), 0, 255);
                    }
                });
            }
            else
            {
                Parallel.For(0, bitmapData.Height, row =>
                {
                    int byteIndex = row*bitmapData.Stride;
                    for (int col = 0; col < bitmapData.Width; col++)
                    {
                        int pixelIndex = byteIndex + col*3;

                        var n = 0;
                        double r1 = 0, g1 = 0, b1 = 0;
                        for (int fi = 0; fi < frameNumber; fi++)
                        {
                            var r = radius - radius*(fi)/frameNumber;
                            for (int i = -r; i <= r; i++)
                            {
                                var row2 = ColorModel.GetValue(row + i, 0, bitmapData.Height - 1)*bitmapData.Stride;
                                for (int j = -r; j <= r; j++)
                                {
                                    var col2 = ColorModel.GetValue(col + j, 0, bitmapData.Width - 1)*3;

                                    r1 += 1f/buffers[fi][row2 + col2 + 2];
                                    g1 += 1f/buffers[fi][row2 + col2 + 1];
                                    b1 += 1f/buffers[fi][row2 + col2];
                                    n++;
                                }
                            }
                        }


                        result[pixelIndex + 2] = (byte) ColorModel.GetValue((int) (n/r1), 0, 255);
                        result[pixelIndex + 1] = (byte) ColorModel.GetValue((int) (n/g1), 0, 255);
                        result[pixelIndex] = (byte) ColorModel.GetValue((int) (n/b1), 0, 255);
                    }
                });
            }
        }
        #endregion

        #region 2D Cleaner
        public void Filter2DCleaner(Frame frame, Frame[] frames, int radius, int threshold) {
            BitmapData bitmapData = frame.BitmapData;
            var result = frame.Buffer;
            Console.Write("Filter2DCleaner");
            byte[] original = (byte[])result.Clone();
            for (int row = 0; row < bitmapData.Height; row++) {
                int byteIndex = row * bitmapData.Stride;
                for (int col = 0; col < bitmapData.Width; col++) {
                    int pixelIndex = byteIndex + col * 3;
                    double r = original[pixelIndex + 2];
                    double g = original[pixelIndex + 1];
                    double b = original[pixelIndex];
                    int sumR = 0, sumG = 0, sumB = 0;
                    int countR = 0, countG = 0, countB = 0;
                    for (int i = -radius; i <= radius; i++) {
                        var row2 = ColorModel.GetValue(row + i, 0, bitmapData.Height - 1) * bitmapData.Stride;
                        for (int j = -radius; j <= radius; j++) {
                            var col2 = ColorModel.GetValue(col + j, 0, bitmapData.Width - 1) * 3;

                            if (Math.Abs(r - result[row2 + col2 + 2]) < threshold) {
                                sumR += result[row2 + col2 + 2];
                                countR++;
                            }
                            if (Math.Abs(g - result[row2 + col2 + 1]) < threshold) {
                                sumG += result[row2 + col2 + 1];
                                countG++;
                            }
                            if (Math.Abs(b - result[row2 + col2]) < threshold) {
                                sumB += result[row2 + col2];
                                countB++;
                            }
                        }
                    }
                    result[pixelIndex + 2] = GetAv(sumR, countR);
                    result[pixelIndex + 1] = GetAv(sumG, countG);
                    result[pixelIndex] = GetAv(sumB, countB);
                }
            }
        }
            
        #endregion

        #endregion

        #region VOCR

        public DetectorResult VocrControur(Frame frame, int threshold, double gain, int brightnessThreshold, bool show)
        {
            DetectorResult detectorResult = new DetectorResult();
            byte[] buffer = new byte[frame.Width * frame.Height];
            for (int row = 0; row < frame.Height; row++)
            {
                int pixelIndex = row * frame.Stride;
                for (int col = 0; col < frame.Width; col++)
                {
                    buffer[row * frame.Width + col] = (byte) ColorModel.GetBrightness(
                        frame.Buffer[pixelIndex + col * 3 + 2],
                        frame.Buffer[pixelIndex + col * 3 + 1],
                        frame.Buffer[pixelIndex + col * 3]);
                }
            }

            var originalBuffer = (byte[])buffer.Clone();
            for (int row = 0; row < frame.Height; row++) {
                var row1 = row * frame.Width;
                var rows = new[]{
                    ColorModel.GetValue(row - 1, 0, frame.Height - 1) * frame.Width,
                    row1, 
                    ColorModel.GetValue(row + 1, 0, frame.Height - 1) * frame.Width
                };
                for (int col = 0; col < frame.Width; col++) {
                    double sumX = 0, sumY = 0;
                    for (int i = 0; i < 3; i++) {
                        for (int j = 0; j < 3; j++) {
                            var p = rows[i] + ColorModel.GetValue(col + j - 1, 0, frame.Width - 1);
                            var gX = _sobelMatrixGx1[i][j];
                            var gY = _sobelMatrixGy1[i][j];
                            if (gX == 0 && gY == 0) continue;
                            sumX += originalBuffer[p] * gX;
                            sumY += originalBuffer[p] * gY;
                        }
                    }

                    buffer[row1 + col] = (byte)ColorModel.GetValue(OperatorSobel(threshold, gain, sumX, sumY), 0, 255);
                    if (buffer[row1 + col] < brightnessThreshold) buffer[row1 + col] = 0;
                }
            }
            //Второй этап - морфологическая обработка
            originalBuffer = (byte[])buffer.Clone();
            for (int row = 0; row < frame.Height; row++) {
                //int byteIndex = row * frame.Width;
                for (int col = 0; col < frame.Width; col++) {
                    byte val = 0;
                    for (int i = -2; i <= 2; i++) {
                        var row2 = ColorModel.GetValue(row + i, 0, frame.Height - 1) * frame.Width;
                        var y = originalBuffer[row2 + col];
                        if (y > val) val = y;
                    }
                    for (int j = -5; j <= 5; j++) {
                        var col2 = ColorModel.GetValue(col + j, 0, frame.Width - 1);
                        var y = originalBuffer[row * frame.Width + col2];
                        if (y > val) val = y;
                    }
                    if (val > 128) {
                        detectorResult.Add(col, row, 7);
                    }
                    if (show)
                    {
                        var color = (byte)(val > 128 ? 255 : 0);
                        var pixel = row*frame.Stride + col*3;
                        frame.Buffer[pixel + 2] = frame.Buffer[pixel + 1] = frame.Buffer[pixel] = color;
                    }
                }
            }

            return detectorResult;
        }

        private double OperatorSobel(double threshold, double gain, double x, double y) {
            var sum = Math.Abs(x) + Math.Abs(y);
            return sum < threshold ? 0 : sum * gain;
        }

        public DetectorResult VocrColorInformation(Frame frame, int threshold, double gain, int brightnessThreshold, bool show)
        {
            Image<Bgr, Byte> img = new Image<Bgr, byte>((Bitmap)frame.Image.Clone());
            var imgGray = img.Convert<Gray, Byte>();
            var imgSobel = imgGray.Sobel(1, 0, 3).Convert<Gray, Byte>();
            var imgRes = new Image<Gray, byte>(imgSobel.Size);
            CvInvoke.Threshold(imgSobel, imgRes, 0, 255, ThresholdType.Binary | ThresholdType.Otsu);
            var element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2, 2), new Point(-1, -1));
            CvInvoke.Dilate(imgRes, imgRes, element, new Point(0, 0), 2, BorderType.Default, new MCvScalar(0));
            CvInvoke.Erode(imgRes, imgRes, element, new Point(0, 0), 16, BorderType.Default, new MCvScalar(0));

            DetectorResult detectorResult = new DetectorResult();
            using (Mat hierachy = new Mat())
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint()) {
                CvInvoke.FindContours(imgRes, contours, hierachy, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                for (int i = 0; i < contours.Size; i++) {
                    Rectangle rectangle = CvInvoke.BoundingRectangle(contours[i]);
                    var area = rectangle.Width * rectangle.Height;
                    if (area > 1000){//} && rectangle.Width < img.Width * 0.7 && rectangle.Width > rectangle.Height * 1.5) {
                        detectorResult.Regions.Add(new DetectorRegion(rectangle));
                    }
                }
            }
            return detectorResult;
        }

        #endregion

        #region Help methods
        private byte GetAv(double sum, double k) {
            sum /= k;
            if (sum < 0) sum = 0;
            else if (sum > 255) sum = 255;
            return (byte)sum;
        }

        int GetFilterSize(int radius) {
            return (radius * 2 + 1) * (radius * 2 + 1);
        }

        #region Kernels
        public double[] GetGausKernel(int radius, double sigma) {
            var kernelSize = GetFilterSize(radius);
            var sum = 0d;
            int k = 0;
            double kk = 1d / (2 * Math.PI * sigma * sigma);
            var gaussKernel = new double[kernelSize];
            for (int i = -radius; i <= radius; i++) {
                for (int j = -radius; j <= radius; j++) {
                    var val = kk * Math.Exp(-(i * i + j * j) / (2 * sigma * sigma));
                    gaussKernel[k] = val;
                    sum += val;
                    k++;
                }
            }
            for (int i = 0; i < gaussKernel.Length; i++) gaussKernel[i] /= sum;
            return gaussKernel;
        }

        private bool[] GetCircleStructureElement(int radius) {
            var kernelSize = 2 * radius + 1;
            int k = 0;
            var kernel = new bool[kernelSize * kernelSize];
            for (int i = -radius; i <= radius; i++) {
                for (int j = -radius; j <= radius; j++) {
                    kernel[k] = Math.Sqrt(i * i + j * j) <= radius;
                    k++;
                }
            }
            return kernel;
        }
        #endregion

        #endregion
    }
}