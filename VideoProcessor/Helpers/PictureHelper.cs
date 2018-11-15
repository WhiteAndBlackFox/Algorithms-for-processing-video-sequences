using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VideoProcessor.Helpers
{
    public static class PictureHelper
    {
        public static Point? GetMousePosition(this PictureBox pictureBox, Point coordinates)
        {
            var image = pictureBox.Image;
            // test to make sure our image is not null
            if (image == null) return coordinates;
            // Make sure our control width and height are not 0 and our 
            // image width and height are not 0
            if (pictureBox.Width == 0 || pictureBox.Height == 0 || image.Width == 0 || image.Height == 0) return coordinates;
            // This is the one that gets a little tricky. Essentially, need to check 
            // the aspect ratio of the image to the aspect ratio of the control
            // to determine how it is being rendered
            float imageAspect = (float)image.Width / image.Height;
            float controlAspect = (float)pictureBox.Width / pictureBox.Height;
            float newX = coordinates.X;
            float newY = coordinates.Y;
            if (imageAspect > controlAspect)
            {
                // This means that we are limited by width, 
                // meaning the image fills up the entire control from left to right
                float ratioWidth = (float)image.Width / pictureBox.Width;
                newX *= ratioWidth;
                float scale = (float)pictureBox.Width / image.Width;
                float displayHeight = scale * image.Height;
                float diffHeight = pictureBox.Height - displayHeight;
                diffHeight /= 2;
                newY -= diffHeight;
                newY /= scale;
            }
            else
            {
                // This means that we are limited by height, 
                // meaning the image fills up the entire control from top to bottom
                float ratioHeight = (float)image.Height / pictureBox.Height;
                newY *= ratioHeight;
                float scale = (float)pictureBox.Height / image.Height;
                float displayWidth = scale * image.Width;
                float diffWidth = pictureBox.Width - displayWidth;
                diffWidth /= 2;
                newX -= diffWidth;
                newX /= scale;
            }
            return newX < 0 || newX > image.Width || newY < 0 || newY > image.Height ? (Point?)null : new Point((int)newX, (int)newY);
        }

        public static Bitmap CreateNew(this Bitmap srcBitmap, byte[] buffer)
        {
            var bitmap = (Bitmap) srcBitmap.Clone();
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            //Блокируем байты изображения на время манупуляций
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            IntPtr ptr = bitmapData.Scan0;
            //Копируем обратно
            Marshal.Copy(buffer, 0, ptr, buffer.Length);
            //Разблокируем байты изображения
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }
    }
}
