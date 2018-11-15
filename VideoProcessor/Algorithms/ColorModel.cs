using System;

namespace VideoProcessor.Algorithms
{
    //RGB, HLS или HSV, YUV[YCbCr]

    public static class ColorModel
    {
        public static string[] RgbNames = { "R", "G", "B" };
        public static string[] HsvNames = { "H", "S", "V" };
        public static string[] YuvNames = { "Y", "U", "V" };
        public const float Kr = 0.299f;
        public const float Kb = 0.114f;

        #region Hsv
        public static void ToHsv(byte r, byte g, byte b, out double hue, out double saturation, out double value) {
            int max = Math.Max(r, Math.Max(g, b));
            int min = Math.Min(r, Math.Min(g, b));
            hue = GetHue(r,g,b);
            saturation = max == 0 ? 0 : 1 - (1d * min / max);
            value = max / 255d;
        }

        public static void FromHsv(double hue, double saturation, double value, out byte r, out byte g, out byte b)
        {
            hue = GetValue(hue, 0, 360);
            saturation = GetValue(saturation, 0, 1);
            value = GetValue(value, 0, 1);
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = (byte)value;
            byte p = (byte)(value * (1 - saturation));
            byte q = (byte)(value * (1 - f * saturation));
            byte t = (byte)(value * (1 - (1 - f) * saturation));
            if (hi == 0)
            {
                r = v;
                g = t;
                b = p;
            }
            else if (hi == 2)
            {
                r = q;
                g = v;
                b = p;
            }
            else if (hi == 3)
            {
                r = p;
                g = q;
                b = v;
            }
            else if (hi == 4)
            {
                r = t;
                g = p;
                b = v;
            }
            else
            {
                r = v;
                g = p;
                b = q;
            }
        }
        #endregion

        #region YUV

        public static double GetBrightness(byte r, byte g, byte b) {
            return Kr * r + 0.587 * g + Kb * b;
        }

        public static int GetColor(byte r, byte g, byte b)
        {
            return (r << 16) + (g << 8) + b;
        }

        public static void ToYuv(byte r, byte g, byte b, out double y, out double u, out double v)
        {
            y = GetBrightness(r, g, b);
            u = -0.147 * r - 0.289 * g + 0.436 * b;
            v = 0.615 * r - 0.515 * g - 0.1 * b;
        }

        public static void FromYuv(double y, double u, double v, out byte r, out byte g, out byte b)
        {
            y = GetValue(y, 0, 255);
            u = GetValue(u, -112, 112);
            v = GetValue(v, -157, 157);
            r = (byte)GetValue(y + 1.14 * v, 0, 255);
            g = (byte)GetValue(y - 0.395 * u - 0.581 * v, 0, 255);
            b = (byte)GetValue(y + 2.032 * u, 0, 255);
        }

        #endregion

        #region YCbCr
        public static void ToYCbCr(byte r, byte g, byte b, out double y, out double cb, out double cr)
        {
            y = GetBrightness(r, g, b);
            cb = 128 - 0.168736 * r - 0.331264 * g + 0.5 * b;
            cr = 128 + 0.5 * r - 0.418688 * g - 0.081312 * b;
        }

        public static void FromYCbCr(double y, double cb, double cr, out byte r, out byte g, out byte b)
        {
            r = (byte)GetValue(y + 1.402 * (cr - 128),0,255);
            g = (byte)GetValue(y - 0.34414 * (cb - 128) - 0.71414 * (cr - 128),0,255);
            b = (byte)GetValue(y + 1.1772 * (cb - 128),0,255);
        }
        #endregion

        private static double GetHue(byte r, byte g, byte b)
        {
            if (r == g && g == b) return 0.0f;
            float num1 = (float) r / byte.MaxValue;
            float num2 = (float) g / byte.MaxValue;
            float num3 = (float) b / byte.MaxValue;
            float num4 = 0.0f;
            float num5 = num1;
            float num6 = num1;
            if (num2 > num5)
            num5 = num2;
            if (num3 > num5)
            num5 = num3;
            if(num2 < num6)
            num6 = num2;
            if (num3 < num6)
            num6 = num3;
            float num7 = num5 - num6;
            if(num1 == num5)
            num4 = (num2 - num3) / num7;
            else if(num2 == num5)
            num4 = (float) (2.0 + (num3 - num1) / (double) num7);
            else if (num3 == num5)
            num4 = (float) (4.0 + (num1 - (double) num2) / num7);
            float num8 = num4 * 60f;
            if (num8 < 0.0)
            num8 += 360f;
            return num8;
        }

        public static double GetValue(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double GetValue(double value)
        {
            return GetValue(value, 0, 255);
        }

        public static int GetValue(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int GetValue(int value)
        {
            return GetValue(value, 0, 255);
        }
    }
}