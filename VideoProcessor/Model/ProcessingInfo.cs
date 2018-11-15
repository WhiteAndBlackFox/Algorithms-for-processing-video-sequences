using System;
using System.Linq;

namespace VideoProcessor.Model
{
    public class ProcessingInfo
    {
        public double Mse { get; set; }
        public double Psnr { get; set; }
        public double Bfm { get; set; }
        public double Time { get; set; }

        public ProcessingInfo(double time, Frame frame1, Frame frame2)
        {
            Time = time;
            var buffer1 = frame1.Buffer;
            var buffer2 = frame2.Buffer;
            Mse = buffer1.Select((t, i) => Math.Pow(buffer2[i] - t, 2)).Sum() / buffer1.Length;
            Psnr = Mse == 0 ? 0 : 10f * Math.Log10(65536) / Mse;
            Bfm = Math.Abs(frame1.GetAverageBrightness() - frame2.GetAverageBrightness());
        }

        public double GetPsnr(double mse) {
            return mse == 0 ? 0 : 10f * Math.Log10(65536) / mse;
        }
    }
}