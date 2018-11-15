using System;

namespace VideoProcessor.Algorithms
{
    public static class GrayScale
    {
        public static byte FromRgb(byte r, byte g, byte b) {
            return (byte)(r * 0.30f + g * 0.59f + b * 0.11f);
        }

        public static byte FromYuv(byte r, byte g, byte b) {
            //Y channel
            return (byte)(ColorModel.Kr * r + 0.587 * g + ColorModel.Kb * b);
        }

        public static byte FromHsv(byte r, byte g, byte b) {
            byte max = Math.Max(r, Math.Max(g, b));
            byte min = Math.Min(r, Math.Min(g, b));
            //S channel
            return (byte)(max == 0 ? 0 : 1 - (1d * min / max) * 255);
        }
    }
}