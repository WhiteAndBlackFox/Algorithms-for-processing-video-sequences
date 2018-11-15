namespace VideoProcessor.Features.FeaturesDetector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;
    using AForge;
    using AForge.Imaging;
    using AForge.Imaging.Filters;

    [Serializable]
    [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
    public class FastCornersDetector : ICornersDetector
    {
        private int threshold = 20;
        private bool suppress = true;
        private int[] scores;

        #region Constructors

        public FastCornersDetector()
        {
        }

        public FastCornersDetector(int threshold)
        {
            this.threshold = threshold;
        }
        #endregion


        #region Properties

        public bool Suppress
        {
            get { return suppress; }
            set { suppress = value; }
        }

        public int Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }

        public int[] Scores
        {
            get { return scores; }
        }
        #endregion


        public List<IntPoint> ProcessImage(BitmapData imageData)
        {
            return ProcessImage(new UnmanagedImage(imageData));
        }

        public List<IntPoint> ProcessImage(Bitmap image)
        {
            // check image format
            if (
                (image.PixelFormat != PixelFormat.Format8bppIndexed) &&
                (image.PixelFormat != PixelFormat.Format24bppRgb) &&
                (image.PixelFormat != PixelFormat.Format32bppRgb) &&
                (image.PixelFormat != PixelFormat.Format32bppArgb)
                )
            {
                throw new UnsupportedImageFormatException("Unsupported pixel format of the source");
            }

            // lock source image
            BitmapData imageData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);

            List<IntPoint> corners;

            try
            {
                // process the image
                corners = ProcessImage(new UnmanagedImage(imageData));
            }
            finally
            {
                // unlock image
                image.UnlockBits(imageData);
            }

            return corners;
        }

        public List<IntPoint> ProcessImage(UnmanagedImage image)
        {

            // check image format
            if (
                (image.PixelFormat != PixelFormat.Format8bppIndexed) &&
                (image.PixelFormat != PixelFormat.Format24bppRgb) &&
                (image.PixelFormat != PixelFormat.Format32bppRgb) &&
                (image.PixelFormat != PixelFormat.Format32bppArgb)
                )
            {
                throw new UnsupportedImageFormatException("Unsupported pixel format of the source image.");
            }

            // make sure we have grayscale image
            UnmanagedImage grayImage = null;

            if (image.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                grayImage = image;
            }
            else
            {
                // create temporary grayscale image
                grayImage = Grayscale.CommonAlgorithms.BT709.Apply(image);
            }


            // 0. Cache pixel offsets            
            int[] offsets = makeOffsets(grayImage.Stride);


            // 1. Detect corners using the given threshold
            IntPoint[] corners = detect(grayImage, offsets);


            // 2. Compute scores for each corner
            int[] scores = new int[corners.Length];
            for (int i = 0; i < corners.Length; i++)
                scores[i] = score(grayImage, corners[i], offsets);


            if (suppress)
            {
                // 3. Perform Non-Maximum Suppression
                int[] idx = maximum(corners, scores);
                corners = corners.Submatrix(idx);
                scores = scores.Submatrix(idx);
            }

            this.scores = scores;
            return corners.ToList();
        }

        #region Private methods
        private static int[] maximum(IntPoint[] corners, int[] scores)
        {
            int n = corners.Length;

            List<int> maximum = new List<int>(n);

            if (corners.Length == 0)
                return maximum.ToArray();


            int last_row;
            int[] row_start;

            // Point above points (roughly) to the pixel above
            // the one of interest, if there is a feature there.
            int point_above = 0;
            int point_below = 0;


            // Find where each row begins (the corners are output in raster scan order).
            // A beginning of -1 signifies that there are no corners on that row.
            last_row = corners[n - 1].Y;
            row_start = new int[last_row + 1];

            for (int i = 0; i < last_row + 1; i++)
                row_start[i] = -1;

            int prev_row = -1;
            for (int i = 0; i < n; i++)
            {
                if (corners[i].Y != prev_row)
                {
                    row_start[corners[i].Y] = i;
                    prev_row = corners[i].Y;
                }
            }


            // for each detected corner
            for (int i = 0; i < n; i++)
            {
                int score = scores[i];
                IntPoint pos = corners[i];

                // Check left
                if (i > 0)
                    if (corners[i - 1].X == pos.X - 1 &&
                        corners[i - 1].Y == pos.Y && scores[i - 1] >= score)
                        continue;

                // Check right
                if (i < (n - 1))
                    if (corners[i + 1].X == pos.X + 1 &&
                        corners[i + 1].Y == pos.Y && scores[i + 1] >= score)
                        continue;

                // Check above (if there is a valid row above)
                if (pos.Y != 0 && row_start[pos.Y - 1] != -1)
                {
                    // Make sure that current point_above is one row above.
                    if (corners[point_above].Y < pos.Y - 1)
                        point_above = row_start[pos.Y - 1];

                    // Make point_above point to the first of the pixels above the current point, if it exists.*/
                    for (; corners[point_above].Y < pos.Y && corners[point_above].X < pos.X - 1; point_above++) ;

                    for (int j = point_above; corners[j].Y < pos.Y && corners[j].X <= pos.X + 1; j++)
                    {
                        int x = corners[j].X;
                        if ((x == pos.X - 1 || x == pos.X || x == pos.X + 1) && scores[j] >= score)
                            goto next_corner;
                    }

                }

                // Check below (if there is anything below)
                if (pos.Y != last_row && row_start[pos.Y + 1] != -1 && point_below < n)
                {
                    // Nothing below
                    if (corners[point_below].Y < pos.Y + 1)
                        point_below = row_start[pos.Y + 1];

                    // Make point below point to one of the pixels below the current point, if it exists.
                    for (; point_below < n && corners[point_below].Y == pos.Y + 1 && corners[point_below].X < pos.X - 1; point_below++) ;

                    for (int j = point_below; j < n && corners[j].Y == pos.Y + 1 && corners[j].X <= pos.X + 1; j++)
                    {
                        int x = corners[j].X;
                        if ((x == pos.X - 1 || x == pos.X || x == pos.X + 1) && scores[j] >= score)
                            goto next_corner;
                    }
                }

                // The current point is a local maximum.
                // Add its index to the list of indices.
                maximum.Add(i);

            next_corner:
                continue;
            }

            return maximum.ToArray();
        }

        private unsafe IntPoint[] detect(UnmanagedImage image, int[] offsets)
        {
            int width = image.Width;
            int height = image.Height;
            int stride = image.Stride;
            int offset = stride - width;
            int b = this.threshold;

            byte* src = (byte*)image.ImageData.ToPointer();
            byte* p = src + 3 * stride + 3;

            List<IntPoint> points = new List<IntPoint>(512);

            for (int y = 3; y < height - 3; y++)
            {
                for (int x = 3; x < width - 3; x++, p++)
                {

                    #region Machine Generated Code
                    int cb = *p + b;
                    int c_b = *p - b;
                    if (p[offsets[0]] > cb)
                        if (p[offsets[1]] > cb)
                            if (p[offsets[2]] > cb)
                                if (p[offsets[3]] > cb)
                                    if (p[offsets[4]] > cb)
                                        if (p[offsets[5]] > cb)
                                            if (p[offsets[6]] > cb)
                                                if (p[offsets[7]] > cb)
                                                    if (p[offsets[8]] > cb)
                                                    { }
                                                    else
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            continue;
                                                else if (p[offsets[7]] < c_b)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            continue;
                                                    else if (p[offsets[14]] < c_b)
                                                        if (p[offsets[8]] < c_b)
                                                            if (p[offsets[9]] < c_b)
                                                                if (p[offsets[10]] < c_b)
                                                                    if (p[offsets[11]] < c_b)
                                                                        if (p[offsets[12]] < c_b)
                                                                            if (p[offsets[13]] < c_b)
                                                                                if (p[offsets[15]] < c_b)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (p[offsets[6]] < c_b)
                                                if (p[offsets[15]] > cb)
                                                    if (p[offsets[13]] > cb)
                                                        if (p[offsets[14]] > cb)
                                                        { }
                                                        else
                                                            continue;
                                                    else if (p[offsets[13]] < c_b)
                                                        if (p[offsets[7]] < c_b)
                                                            if (p[offsets[8]] < c_b)
                                                                if (p[offsets[9]] < c_b)
                                                                    if (p[offsets[10]] < c_b)
                                                                        if (p[offsets[11]] < c_b)
                                                                            if (p[offsets[12]] < c_b)
                                                                                if (p[offsets[14]] < c_b)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (p[offsets[7]] < c_b)
                                                        if (p[offsets[8]] < c_b)
                                                            if (p[offsets[9]] < c_b)
                                                                if (p[offsets[10]] < c_b)
                                                                    if (p[offsets[11]] < c_b)
                                                                        if (p[offsets[12]] < c_b)
                                                                            if (p[offsets[13]] < c_b)
                                                                                if (p[offsets[14]] < c_b)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (p[offsets[13]] < c_b)
                                                    if (p[offsets[7]] < c_b)
                                                        if (p[offsets[8]] < c_b)
                                                            if (p[offsets[9]] < c_b)
                                                                if (p[offsets[10]] < c_b)
                                                                    if (p[offsets[11]] < c_b)
                                                                        if (p[offsets[12]] < c_b)
                                                                            if (p[offsets[14]] < c_b)
                                                                                if (p[offsets[15]] < c_b)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (p[offsets[5]] < c_b)
                                            if (p[offsets[14]] > cb)
                                                if (p[offsets[12]] > cb)
                                                    if (p[offsets[13]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                            if (p[offsets[10]] > cb)
                                                                                if (p[offsets[11]] > cb)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else if (p[offsets[12]] < c_b)
                                                    if (p[offsets[6]] < c_b)
                                                        if (p[offsets[7]] < c_b)
                                                            if (p[offsets[8]] < c_b)
                                                                if (p[offsets[9]] < c_b)
                                                                    if (p[offsets[10]] < c_b)
                                                                        if (p[offsets[11]] < c_b)
                                                                            if (p[offsets[13]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (p[offsets[14]] < c_b)
                                                if (p[offsets[7]] < c_b)
                                                    if (p[offsets[8]] < c_b)
                                                        if (p[offsets[9]] < c_b)
                                                            if (p[offsets[10]] < c_b)
                                                                if (p[offsets[11]] < c_b)
                                                                    if (p[offsets[12]] < c_b)
                                                                        if (p[offsets[13]] < c_b)
                                                                            if (p[offsets[6]] < c_b)
                                                                            { }
                                                                            else
                                                                                if (p[offsets[15]] < c_b)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (p[offsets[6]] < c_b)
                                                    if (p[offsets[7]] < c_b)
                                                        if (p[offsets[8]] < c_b)
                                                            if (p[offsets[9]] < c_b)
                                                                if (p[offsets[10]] < c_b)
                                                                    if (p[offsets[11]] < c_b)
                                                                        if (p[offsets[12]] < c_b)
                                                                            if (p[offsets[13]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                            if (p[offsets[10]] > cb)
                                                                                if (p[offsets[11]] > cb)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (p[offsets[12]] < c_b)
                                                if (p[offsets[7]] < c_b)
                                                    if (p[offsets[8]] < c_b)
                                                        if (p[offsets[9]] < c_b)
                                                            if (p[offsets[10]] < c_b)
                                                                if (p[offsets[11]] < c_b)
                                                                    if (p[offsets[13]] < c_b)
                                                                        if (p[offsets[14]] < c_b)
                                                                            if (p[offsets[6]] < c_b)
                                                                            { }
                                                                            else
                                                                                if (p[offsets[15]] < c_b)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (p[offsets[4]] < c_b)
                                        if (p[offsets[13]] > cb)
                                            if (p[offsets[11]] > cb)
                                                if (p[offsets[12]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                            if (p[offsets[10]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                            if (p[offsets[10]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else if (p[offsets[11]] < c_b)
                                                if (p[offsets[5]] < c_b)
                                                    if (p[offsets[6]] < c_b)
                                                        if (p[offsets[7]] < c_b)
                                                            if (p[offsets[8]] < c_b)
                                                                if (p[offsets[9]] < c_b)
                                                                    if (p[offsets[10]] < c_b)
                                                                        if (p[offsets[12]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (p[offsets[13]] < c_b)
                                            if (p[offsets[7]] < c_b)
                                                if (p[offsets[8]] < c_b)
                                                    if (p[offsets[9]] < c_b)
                                                        if (p[offsets[10]] < c_b)
                                                            if (p[offsets[11]] < c_b)
                                                                if (p[offsets[12]] < c_b)
                                                                    if (p[offsets[6]] < c_b)
                                                                        if (p[offsets[5]] < c_b)
                                                                        { }
                                                                        else
                                                                            if (p[offsets[14]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                    else
                                                                        if (p[offsets[14]] < c_b)
                                                                            if (p[offsets[15]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (p[offsets[5]] < c_b)
                                                if (p[offsets[6]] < c_b)
                                                    if (p[offsets[7]] < c_b)
                                                        if (p[offsets[8]] < c_b)
                                                            if (p[offsets[9]] < c_b)
                                                                if (p[offsets[10]] < c_b)
                                                                    if (p[offsets[11]] < c_b)
                                                                        if (p[offsets[12]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                            if (p[offsets[10]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                            if (p[offsets[10]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (p[offsets[11]] < c_b)
                                            if (p[offsets[7]] < c_b)
                                                if (p[offsets[8]] < c_b)
                                                    if (p[offsets[9]] < c_b)
                                                        if (p[offsets[10]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                    if (p[offsets[6]] < c_b)
                                                                        if (p[offsets[5]] < c_b)
                                                                        { }
                                                                        else
                                                                            if (p[offsets[14]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                    else
                                                                        if (p[offsets[14]] < c_b)
                                                                            if (p[offsets[15]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (p[offsets[3]] < c_b)
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (p[offsets[10]] < c_b)
                                        if (p[offsets[7]] < c_b)
                                            if (p[offsets[8]] < c_b)
                                                if (p[offsets[9]] < c_b)
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[5]] < c_b)
                                                                if (p[offsets[4]] < c_b)
                                                                { }
                                                                else
                                                                    if (p[offsets[12]] < c_b)
                                                                        if (p[offsets[13]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                if (p[offsets[12]] < c_b)
                                                                    if (p[offsets[13]] < c_b)
                                                                        if (p[offsets[14]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                    if (p[offsets[14]] < c_b)
                                                                        if (p[offsets[15]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                        if (p[offsets[9]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (p[offsets[10]] < c_b)
                                        if (p[offsets[7]] < c_b)
                                            if (p[offsets[8]] < c_b)
                                                if (p[offsets[9]] < c_b)
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[5]] < c_b)
                                                                    if (p[offsets[4]] < c_b)
                                                                    { }
                                                                    else
                                                                        if (p[offsets[13]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                else
                                                                    if (p[offsets[13]] < c_b)
                                                                        if (p[offsets[14]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                if (p[offsets[13]] < c_b)
                                                                    if (p[offsets[14]] < c_b)
                                                                        if (p[offsets[15]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else if (p[offsets[2]] < c_b)
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[3]] > cb)
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (p[offsets[9]] < c_b)
                                    if (p[offsets[7]] < c_b)
                                        if (p[offsets[8]] < c_b)
                                            if (p[offsets[10]] < c_b)
                                                if (p[offsets[6]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[4]] < c_b)
                                                            if (p[offsets[3]] < c_b)
                                                            { }
                                                            else
                                                                if (p[offsets[11]] < c_b)
                                                                    if (p[offsets[12]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (p[offsets[11]] < c_b)
                                                                if (p[offsets[12]] < c_b)
                                                                    if (p[offsets[13]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[11]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                    if (p[offsets[14]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[14]] < c_b)
                                                                    if (p[offsets[15]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[3]] > cb)
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                    if (p[offsets[8]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (p[offsets[9]] < c_b)
                                    if (p[offsets[7]] < c_b)
                                        if (p[offsets[8]] < c_b)
                                            if (p[offsets[10]] < c_b)
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[6]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[4]] < c_b)
                                                                if (p[offsets[3]] < c_b)
                                                                { }
                                                                else
                                                                    if (p[offsets[12]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                            else
                                                                if (p[offsets[12]] < c_b)
                                                                    if (p[offsets[13]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                    if (p[offsets[14]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[14]] < c_b)
                                                                    if (p[offsets[15]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else if (p[offsets[1]] < c_b)
                            if (p[offsets[8]] > cb)
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[3]] > cb)
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[2]] > cb)
                                                if (p[offsets[3]] > cb)
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (p[offsets[8]] < c_b)
                                if (p[offsets[7]] < c_b)
                                    if (p[offsets[9]] < c_b)
                                        if (p[offsets[6]] < c_b)
                                            if (p[offsets[5]] < c_b)
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[3]] < c_b)
                                                        if (p[offsets[2]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[10]] < c_b)
                                                                if (p[offsets[11]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[10]] < c_b)
                                                            if (p[offsets[11]] < c_b)
                                                                if (p[offsets[12]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[10]] < c_b)
                                                        if (p[offsets[11]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[10]] < c_b)
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[14]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[10]] < c_b)
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[13]] < c_b)
                                                            if (p[offsets[14]] < c_b)
                                                                if (p[offsets[15]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                continue;
                        else
                            if (p[offsets[8]] > cb)
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[3]] > cb)
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[2]] > cb)
                                                if (p[offsets[3]] > cb)
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[7]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (p[offsets[8]] < c_b)
                                if (p[offsets[7]] < c_b)
                                    if (p[offsets[9]] < c_b)
                                        if (p[offsets[10]] < c_b)
                                            if (p[offsets[6]] < c_b)
                                                if (p[offsets[5]] < c_b)
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[3]] < c_b)
                                                            if (p[offsets[2]] < c_b)
                                                            { }
                                                            else
                                                                if (p[offsets[11]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                        else
                                                            if (p[offsets[11]] < c_b)
                                                                if (p[offsets[12]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[11]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[14]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[13]] < c_b)
                                                            if (p[offsets[14]] < c_b)
                                                                if (p[offsets[15]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                continue;
                    else if (p[offsets[0]] < c_b)
                        if (p[offsets[1]] > cb)
                            if (p[offsets[8]] > cb)
                                if (p[offsets[7]] > cb)
                                    if (p[offsets[9]] > cb)
                                        if (p[offsets[6]] > cb)
                                            if (p[offsets[5]] > cb)
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[3]] > cb)
                                                        if (p[offsets[2]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[10]] > cb)
                                                                if (p[offsets[11]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[10]] > cb)
                                                            if (p[offsets[11]] > cb)
                                                                if (p[offsets[12]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[10]] > cb)
                                                        if (p[offsets[11]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[10]] > cb)
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[14]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[10]] > cb)
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[13]] > cb)
                                                            if (p[offsets[14]] > cb)
                                                                if (p[offsets[15]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (p[offsets[8]] < c_b)
                                if (p[offsets[9]] < c_b)
                                    if (p[offsets[10]] < c_b)
                                        if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[3]] < c_b)
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[2]] < c_b)
                                                if (p[offsets[3]] < c_b)
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                continue;
                        else if (p[offsets[1]] < c_b)
                            if (p[offsets[2]] > cb)
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[7]] > cb)
                                        if (p[offsets[8]] > cb)
                                            if (p[offsets[10]] > cb)
                                                if (p[offsets[6]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[4]] > cb)
                                                            if (p[offsets[3]] > cb)
                                                            { }
                                                            else
                                                                if (p[offsets[11]] > cb)
                                                                    if (p[offsets[12]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (p[offsets[11]] > cb)
                                                                if (p[offsets[12]] > cb)
                                                                    if (p[offsets[13]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[11]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                    if (p[offsets[14]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[14]] > cb)
                                                                    if (p[offsets[15]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (p[offsets[9]] < c_b)
                                    if (p[offsets[10]] < c_b)
                                        if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[3]] < c_b)
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (p[offsets[2]] < c_b)
                                if (p[offsets[3]] > cb)
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[7]] > cb)
                                            if (p[offsets[8]] > cb)
                                                if (p[offsets[9]] > cb)
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[5]] > cb)
                                                                if (p[offsets[4]] > cb)
                                                                { }
                                                                else
                                                                    if (p[offsets[12]] > cb)
                                                                        if (p[offsets[13]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                if (p[offsets[12]] > cb)
                                                                    if (p[offsets[13]] > cb)
                                                                        if (p[offsets[14]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                    if (p[offsets[14]] > cb)
                                                                        if (p[offsets[15]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (p[offsets[10]] < c_b)
                                        if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (p[offsets[3]] < c_b)
                                    if (p[offsets[4]] > cb)
                                        if (p[offsets[13]] > cb)
                                            if (p[offsets[7]] > cb)
                                                if (p[offsets[8]] > cb)
                                                    if (p[offsets[9]] > cb)
                                                        if (p[offsets[10]] > cb)
                                                            if (p[offsets[11]] > cb)
                                                                if (p[offsets[12]] > cb)
                                                                    if (p[offsets[6]] > cb)
                                                                        if (p[offsets[5]] > cb)
                                                                        { }
                                                                        else
                                                                            if (p[offsets[14]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                    else
                                                                        if (p[offsets[14]] > cb)
                                                                            if (p[offsets[15]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (p[offsets[13]] < c_b)
                                            if (p[offsets[11]] > cb)
                                                if (p[offsets[5]] > cb)
                                                    if (p[offsets[6]] > cb)
                                                        if (p[offsets[7]] > cb)
                                                            if (p[offsets[8]] > cb)
                                                                if (p[offsets[9]] > cb)
                                                                    if (p[offsets[10]] > cb)
                                                                        if (p[offsets[12]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (p[offsets[11]] < c_b)
                                                if (p[offsets[12]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                            if (p[offsets[10]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                            if (p[offsets[10]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (p[offsets[5]] > cb)
                                                if (p[offsets[6]] > cb)
                                                    if (p[offsets[7]] > cb)
                                                        if (p[offsets[8]] > cb)
                                                            if (p[offsets[9]] > cb)
                                                                if (p[offsets[10]] > cb)
                                                                    if (p[offsets[11]] > cb)
                                                                        if (p[offsets[12]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (p[offsets[4]] < c_b)
                                        if (p[offsets[5]] > cb)
                                            if (p[offsets[14]] > cb)
                                                if (p[offsets[7]] > cb)
                                                    if (p[offsets[8]] > cb)
                                                        if (p[offsets[9]] > cb)
                                                            if (p[offsets[10]] > cb)
                                                                if (p[offsets[11]] > cb)
                                                                    if (p[offsets[12]] > cb)
                                                                        if (p[offsets[13]] > cb)
                                                                            if (p[offsets[6]] > cb)
                                                                            { }
                                                                            else
                                                                                if (p[offsets[15]] > cb)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (p[offsets[14]] < c_b)
                                                if (p[offsets[12]] > cb)
                                                    if (p[offsets[6]] > cb)
                                                        if (p[offsets[7]] > cb)
                                                            if (p[offsets[8]] > cb)
                                                                if (p[offsets[9]] > cb)
                                                                    if (p[offsets[10]] > cb)
                                                                        if (p[offsets[11]] > cb)
                                                                            if (p[offsets[13]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (p[offsets[12]] < c_b)
                                                    if (p[offsets[13]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                            if (p[offsets[10]] < c_b)
                                                                                if (p[offsets[11]] < c_b)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (p[offsets[6]] > cb)
                                                    if (p[offsets[7]] > cb)
                                                        if (p[offsets[8]] > cb)
                                                            if (p[offsets[9]] > cb)
                                                                if (p[offsets[10]] > cb)
                                                                    if (p[offsets[11]] > cb)
                                                                        if (p[offsets[12]] > cb)
                                                                            if (p[offsets[13]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (p[offsets[5]] < c_b)
                                            if (p[offsets[6]] > cb)
                                                if (p[offsets[15]] < c_b)
                                                    if (p[offsets[13]] > cb)
                                                        if (p[offsets[7]] > cb)
                                                            if (p[offsets[8]] > cb)
                                                                if (p[offsets[9]] > cb)
                                                                    if (p[offsets[10]] > cb)
                                                                        if (p[offsets[11]] > cb)
                                                                            if (p[offsets[12]] > cb)
                                                                                if (p[offsets[14]] > cb)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (p[offsets[13]] < c_b)
                                                        if (p[offsets[14]] < c_b)
                                                        { }
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (p[offsets[7]] > cb)
                                                        if (p[offsets[8]] > cb)
                                                            if (p[offsets[9]] > cb)
                                                                if (p[offsets[10]] > cb)
                                                                    if (p[offsets[11]] > cb)
                                                                        if (p[offsets[12]] > cb)
                                                                            if (p[offsets[13]] > cb)
                                                                                if (p[offsets[14]] > cb)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (p[offsets[6]] < c_b)
                                                if (p[offsets[7]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[8]] > cb)
                                                            if (p[offsets[9]] > cb)
                                                                if (p[offsets[10]] > cb)
                                                                    if (p[offsets[11]] > cb)
                                                                        if (p[offsets[12]] > cb)
                                                                            if (p[offsets[13]] > cb)
                                                                                if (p[offsets[15]] > cb)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (p[offsets[7]] < c_b)
                                                    if (p[offsets[8]] < c_b)
                                                    { }
                                                    else
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[7]] > cb)
                                                        if (p[offsets[8]] > cb)
                                                            if (p[offsets[9]] > cb)
                                                                if (p[offsets[10]] > cb)
                                                                    if (p[offsets[11]] > cb)
                                                                        if (p[offsets[12]] > cb)
                                                                            if (p[offsets[14]] > cb)
                                                                                if (p[offsets[15]] > cb)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[7]] > cb)
                                                    if (p[offsets[8]] > cb)
                                                        if (p[offsets[9]] > cb)
                                                            if (p[offsets[10]] > cb)
                                                                if (p[offsets[11]] > cb)
                                                                    if (p[offsets[13]] > cb)
                                                                        if (p[offsets[14]] > cb)
                                                                            if (p[offsets[6]] > cb)
                                                                            { }
                                                                            else
                                                                                if (p[offsets[15]] > cb)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                            if (p[offsets[10]] < c_b)
                                                                                if (p[offsets[11]] < c_b)
                                                                                { }
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[7]] > cb)
                                                if (p[offsets[8]] > cb)
                                                    if (p[offsets[9]] > cb)
                                                        if (p[offsets[10]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                    if (p[offsets[6]] > cb)
                                                                        if (p[offsets[5]] > cb)
                                                                        { }
                                                                        else
                                                                            if (p[offsets[14]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                    else
                                                                        if (p[offsets[14]] > cb)
                                                                            if (p[offsets[15]] > cb)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                            if (p[offsets[10]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                            if (p[offsets[10]] < c_b)
                                                                            { }
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[7]] > cb)
                                            if (p[offsets[8]] > cb)
                                                if (p[offsets[9]] > cb)
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[6]] > cb)
                                                                if (p[offsets[5]] > cb)
                                                                    if (p[offsets[4]] > cb)
                                                                    { }
                                                                    else
                                                                        if (p[offsets[13]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                else
                                                                    if (p[offsets[13]] > cb)
                                                                        if (p[offsets[14]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                if (p[offsets[13]] > cb)
                                                                    if (p[offsets[14]] > cb)
                                                                        if (p[offsets[15]] > cb)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (p[offsets[10]] < c_b)
                                        if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                        if (p[offsets[9]] < c_b)
                                                                        { }
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[7]] > cb)
                                        if (p[offsets[8]] > cb)
                                            if (p[offsets[10]] > cb)
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[6]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[4]] > cb)
                                                                if (p[offsets[3]] > cb)
                                                                { }
                                                                else
                                                                    if (p[offsets[12]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                            else
                                                                if (p[offsets[12]] > cb)
                                                                    if (p[offsets[13]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                    if (p[offsets[14]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[14]] > cb)
                                                                    if (p[offsets[15]] > cb)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (p[offsets[9]] < c_b)
                                    if (p[offsets[10]] < c_b)
                                        if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[3]] < c_b)
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                    if (p[offsets[8]] < c_b)
                                                                    { }
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            if (p[offsets[8]] > cb)
                                if (p[offsets[7]] > cb)
                                    if (p[offsets[9]] > cb)
                                        if (p[offsets[10]] > cb)
                                            if (p[offsets[6]] > cb)
                                                if (p[offsets[5]] > cb)
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[3]] > cb)
                                                            if (p[offsets[2]] > cb)
                                                            { }
                                                            else
                                                                if (p[offsets[11]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                        else
                                                            if (p[offsets[11]] > cb)
                                                                if (p[offsets[12]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[11]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[14]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[13]] > cb)
                                                            if (p[offsets[14]] > cb)
                                                                if (p[offsets[15]] > cb)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (p[offsets[8]] < c_b)
                                if (p[offsets[9]] < c_b)
                                    if (p[offsets[10]] < c_b)
                                        if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[3]] < c_b)
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[2]] < c_b)
                                                if (p[offsets[3]] < c_b)
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[6]] < c_b)
                                                                if (p[offsets[7]] < c_b)
                                                                { }
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                continue;
                    else
                        if (p[offsets[7]] > cb)
                            if (p[offsets[8]] > cb)
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[6]] > cb)
                                        if (p[offsets[5]] > cb)
                                            if (p[offsets[4]] > cb)
                                                if (p[offsets[3]] > cb)
                                                    if (p[offsets[2]] > cb)
                                                        if (p[offsets[1]] > cb)
                                                        { }
                                                        else
                                                            if (p[offsets[10]] > cb)
                                                            { }
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[10]] > cb)
                                                            if (p[offsets[11]] > cb)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[10]] > cb)
                                                        if (p[offsets[11]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[10]] > cb)
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[10]] > cb)
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[13]] > cb)
                                                            if (p[offsets[14]] > cb)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (p[offsets[10]] > cb)
                                            if (p[offsets[11]] > cb)
                                                if (p[offsets[12]] > cb)
                                                    if (p[offsets[13]] > cb)
                                                        if (p[offsets[14]] > cb)
                                                            if (p[offsets[15]] > cb)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                            else
                                continue;
                        else if (p[offsets[7]] < c_b)
                            if (p[offsets[8]] < c_b)
                                if (p[offsets[9]] < c_b)
                                    if (p[offsets[6]] < c_b)
                                        if (p[offsets[5]] < c_b)
                                            if (p[offsets[4]] < c_b)
                                                if (p[offsets[3]] < c_b)
                                                    if (p[offsets[2]] < c_b)
                                                        if (p[offsets[1]] < c_b)
                                                        { }
                                                        else
                                                            if (p[offsets[10]] < c_b)
                                                            { }
                                                            else
                                                                continue;
                                                    else
                                                        if (p[offsets[10]] < c_b)
                                                            if (p[offsets[11]] < c_b)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (p[offsets[10]] < c_b)
                                                        if (p[offsets[11]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (p[offsets[10]] < c_b)
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (p[offsets[10]] < c_b)
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[13]] < c_b)
                                                            if (p[offsets[14]] < c_b)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (p[offsets[10]] < c_b)
                                            if (p[offsets[11]] < c_b)
                                                if (p[offsets[12]] < c_b)
                                                    if (p[offsets[13]] < c_b)
                                                        if (p[offsets[14]] < c_b)
                                                            if (p[offsets[15]] < c_b)
                                                            { }
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                            else
                                continue;
                        else
                            continue;

                    #endregion

                    points.Add(new IntPoint(x, y));
                }
                p += offset + 6;
            }

            return points.ToArray();
        }

        private unsafe int score(UnmanagedImage image, IntPoint corner, int[] offsets)
        {
            byte* src = (byte*)image.ImageData.ToPointer();
            byte* p = src + corner.Y * image.Stride + corner.X;

            // Compute the score using binary search
            int bmin = this.threshold, bmax = 255;
            int b = (bmax + bmin) / 2;

            for (; ; )
            {
                int cb = *p + b;
                int c_b = *p - b;

                #region Machine generated code
                if (p[offsets[0]] > cb)
                    if (p[offsets[1]] > cb)
                        if (p[offsets[2]] > cb)
                            if (p[offsets[3]] > cb)
                                if (p[offsets[4]] > cb)
                                    if (p[offsets[5]] > cb)
                                        if (p[offsets[6]] > cb)
                                            if (p[offsets[7]] > cb)
                                                if (p[offsets[8]] > cb)
                                                    goto is_a_corner;
                                                else
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else if (p[offsets[7]] < c_b)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else if (p[offsets[14]] < c_b)
                                                    if (p[offsets[8]] < c_b)
                                                        if (p[offsets[9]] < c_b)
                                                            if (p[offsets[10]] < c_b)
                                                                if (p[offsets[11]] < c_b)
                                                                    if (p[offsets[12]] < c_b)
                                                                        if (p[offsets[13]] < c_b)
                                                                            if (p[offsets[15]] < c_b)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else if (p[offsets[6]] < c_b)
                                            if (p[offsets[15]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else if (p[offsets[13]] < c_b)
                                                    if (p[offsets[7]] < c_b)
                                                        if (p[offsets[8]] < c_b)
                                                            if (p[offsets[9]] < c_b)
                                                                if (p[offsets[10]] < c_b)
                                                                    if (p[offsets[11]] < c_b)
                                                                        if (p[offsets[12]] < c_b)
                                                                            if (p[offsets[14]] < c_b)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                if (p[offsets[7]] < c_b)
                                                    if (p[offsets[8]] < c_b)
                                                        if (p[offsets[9]] < c_b)
                                                            if (p[offsets[10]] < c_b)
                                                                if (p[offsets[11]] < c_b)
                                                                    if (p[offsets[12]] < c_b)
                                                                        if (p[offsets[13]] < c_b)
                                                                            if (p[offsets[14]] < c_b)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else if (p[offsets[13]] < c_b)
                                                if (p[offsets[7]] < c_b)
                                                    if (p[offsets[8]] < c_b)
                                                        if (p[offsets[9]] < c_b)
                                                            if (p[offsets[10]] < c_b)
                                                                if (p[offsets[11]] < c_b)
                                                                    if (p[offsets[12]] < c_b)
                                                                        if (p[offsets[14]] < c_b)
                                                                            if (p[offsets[15]] < c_b)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else if (p[offsets[5]] < c_b)
                                        if (p[offsets[14]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        if (p[offsets[10]] > cb)
                                                                            if (p[offsets[11]] > cb)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else if (p[offsets[12]] < c_b)
                                                if (p[offsets[6]] < c_b)
                                                    if (p[offsets[7]] < c_b)
                                                        if (p[offsets[8]] < c_b)
                                                            if (p[offsets[9]] < c_b)
                                                                if (p[offsets[10]] < c_b)
                                                                    if (p[offsets[11]] < c_b)
                                                                        if (p[offsets[13]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else if (p[offsets[14]] < c_b)
                                            if (p[offsets[7]] < c_b)
                                                if (p[offsets[8]] < c_b)
                                                    if (p[offsets[9]] < c_b)
                                                        if (p[offsets[10]] < c_b)
                                                            if (p[offsets[11]] < c_b)
                                                                if (p[offsets[12]] < c_b)
                                                                    if (p[offsets[13]] < c_b)
                                                                        if (p[offsets[6]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            if (p[offsets[15]] < c_b)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            if (p[offsets[6]] < c_b)
                                                if (p[offsets[7]] < c_b)
                                                    if (p[offsets[8]] < c_b)
                                                        if (p[offsets[9]] < c_b)
                                                            if (p[offsets[10]] < c_b)
                                                                if (p[offsets[11]] < c_b)
                                                                    if (p[offsets[12]] < c_b)
                                                                        if (p[offsets[13]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        if (p[offsets[10]] > cb)
                                                                            if (p[offsets[11]] > cb)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else if (p[offsets[12]] < c_b)
                                            if (p[offsets[7]] < c_b)
                                                if (p[offsets[8]] < c_b)
                                                    if (p[offsets[9]] < c_b)
                                                        if (p[offsets[10]] < c_b)
                                                            if (p[offsets[11]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                    if (p[offsets[14]] < c_b)
                                                                        if (p[offsets[6]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            if (p[offsets[15]] < c_b)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else if (p[offsets[4]] < c_b)
                                    if (p[offsets[13]] > cb)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        if (p[offsets[10]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        if (p[offsets[10]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else if (p[offsets[11]] < c_b)
                                            if (p[offsets[5]] < c_b)
                                                if (p[offsets[6]] < c_b)
                                                    if (p[offsets[7]] < c_b)
                                                        if (p[offsets[8]] < c_b)
                                                            if (p[offsets[9]] < c_b)
                                                                if (p[offsets[10]] < c_b)
                                                                    if (p[offsets[12]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else if (p[offsets[13]] < c_b)
                                        if (p[offsets[7]] < c_b)
                                            if (p[offsets[8]] < c_b)
                                                if (p[offsets[9]] < c_b)
                                                    if (p[offsets[10]] < c_b)
                                                        if (p[offsets[11]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[6]] < c_b)
                                                                    if (p[offsets[5]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        if (p[offsets[14]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                else
                                                                    if (p[offsets[14]] < c_b)
                                                                        if (p[offsets[15]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        if (p[offsets[5]] < c_b)
                                            if (p[offsets[6]] < c_b)
                                                if (p[offsets[7]] < c_b)
                                                    if (p[offsets[8]] < c_b)
                                                        if (p[offsets[9]] < c_b)
                                                            if (p[offsets[10]] < c_b)
                                                                if (p[offsets[11]] < c_b)
                                                                    if (p[offsets[12]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    if (p[offsets[11]] > cb)
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        if (p[offsets[10]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        if (p[offsets[10]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else if (p[offsets[11]] < c_b)
                                        if (p[offsets[7]] < c_b)
                                            if (p[offsets[8]] < c_b)
                                                if (p[offsets[9]] < c_b)
                                                    if (p[offsets[10]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[6]] < c_b)
                                                                    if (p[offsets[5]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        if (p[offsets[14]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                else
                                                                    if (p[offsets[14]] < c_b)
                                                                        if (p[offsets[15]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                            else if (p[offsets[3]] < c_b)
                                if (p[offsets[10]] > cb)
                                    if (p[offsets[11]] > cb)
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else if (p[offsets[10]] < c_b)
                                    if (p[offsets[7]] < c_b)
                                        if (p[offsets[8]] < c_b)
                                            if (p[offsets[9]] < c_b)
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[6]] < c_b)
                                                        if (p[offsets[5]] < c_b)
                                                            if (p[offsets[4]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                if (p[offsets[12]] < c_b)
                                                                    if (p[offsets[13]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                        else
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                    if (p[offsets[14]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[14]] < c_b)
                                                                    if (p[offsets[15]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                if (p[offsets[10]] > cb)
                                    if (p[offsets[11]] > cb)
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    if (p[offsets[9]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else if (p[offsets[10]] < c_b)
                                    if (p[offsets[7]] < c_b)
                                        if (p[offsets[8]] < c_b)
                                            if (p[offsets[9]] < c_b)
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[5]] < c_b)
                                                                if (p[offsets[4]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    if (p[offsets[13]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                            else
                                                                if (p[offsets[13]] < c_b)
                                                                    if (p[offsets[14]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                        else
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[14]] < c_b)
                                                                    if (p[offsets[15]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                        else if (p[offsets[2]] < c_b)
                            if (p[offsets[9]] > cb)
                                if (p[offsets[10]] > cb)
                                    if (p[offsets[11]] > cb)
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[3]] > cb)
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else if (p[offsets[9]] < c_b)
                                if (p[offsets[7]] < c_b)
                                    if (p[offsets[8]] < c_b)
                                        if (p[offsets[10]] < c_b)
                                            if (p[offsets[6]] < c_b)
                                                if (p[offsets[5]] < c_b)
                                                    if (p[offsets[4]] < c_b)
                                                        if (p[offsets[3]] < c_b)
                                                            goto is_a_corner;
                                                        else
                                                            if (p[offsets[11]] < c_b)
                                                                if (p[offsets[12]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        if (p[offsets[11]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[14]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[13]] < c_b)
                                                            if (p[offsets[14]] < c_b)
                                                                if (p[offsets[15]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else
                            if (p[offsets[9]] > cb)
                                if (p[offsets[10]] > cb)
                                    if (p[offsets[11]] > cb)
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[3]] > cb)
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                if (p[offsets[8]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else if (p[offsets[9]] < c_b)
                                if (p[offsets[7]] < c_b)
                                    if (p[offsets[8]] < c_b)
                                        if (p[offsets[10]] < c_b)
                                            if (p[offsets[11]] < c_b)
                                                if (p[offsets[6]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[4]] < c_b)
                                                            if (p[offsets[3]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                if (p[offsets[12]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                        else
                                                            if (p[offsets[12]] < c_b)
                                                                if (p[offsets[13]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                if (p[offsets[14]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[13]] < c_b)
                                                            if (p[offsets[14]] < c_b)
                                                                if (p[offsets[15]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                    else if (p[offsets[1]] < c_b)
                        if (p[offsets[8]] > cb)
                            if (p[offsets[9]] > cb)
                                if (p[offsets[10]] > cb)
                                    if (p[offsets[11]] > cb)
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[3]] > cb)
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[2]] > cb)
                                            if (p[offsets[3]] > cb)
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else if (p[offsets[8]] < c_b)
                            if (p[offsets[7]] < c_b)
                                if (p[offsets[9]] < c_b)
                                    if (p[offsets[6]] < c_b)
                                        if (p[offsets[5]] < c_b)
                                            if (p[offsets[4]] < c_b)
                                                if (p[offsets[3]] < c_b)
                                                    if (p[offsets[2]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[10]] < c_b)
                                                            if (p[offsets[11]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[10]] < c_b)
                                                        if (p[offsets[11]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[10]] < c_b)
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[10]] < c_b)
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[13]] < c_b)
                                                            if (p[offsets[14]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[10]] < c_b)
                                            if (p[offsets[11]] < c_b)
                                                if (p[offsets[12]] < c_b)
                                                    if (p[offsets[13]] < c_b)
                                                        if (p[offsets[14]] < c_b)
                                                            if (p[offsets[15]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else
                            goto is_not_a_corner;
                    else
                        if (p[offsets[8]] > cb)
                            if (p[offsets[9]] > cb)
                                if (p[offsets[10]] > cb)
                                    if (p[offsets[11]] > cb)
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[15]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[3]] > cb)
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[2]] > cb)
                                            if (p[offsets[3]] > cb)
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[7]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else if (p[offsets[8]] < c_b)
                            if (p[offsets[7]] < c_b)
                                if (p[offsets[9]] < c_b)
                                    if (p[offsets[10]] < c_b)
                                        if (p[offsets[6]] < c_b)
                                            if (p[offsets[5]] < c_b)
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[3]] < c_b)
                                                        if (p[offsets[2]] < c_b)
                                                            goto is_a_corner;
                                                        else
                                                            if (p[offsets[11]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        if (p[offsets[11]] < c_b)
                                                            if (p[offsets[12]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            if (p[offsets[13]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[13]] < c_b)
                                                            if (p[offsets[14]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[11]] < c_b)
                                                if (p[offsets[12]] < c_b)
                                                    if (p[offsets[13]] < c_b)
                                                        if (p[offsets[14]] < c_b)
                                                            if (p[offsets[15]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else
                            goto is_not_a_corner;
                else if (p[offsets[0]] < c_b)
                    if (p[offsets[1]] > cb)
                        if (p[offsets[8]] > cb)
                            if (p[offsets[7]] > cb)
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[6]] > cb)
                                        if (p[offsets[5]] > cb)
                                            if (p[offsets[4]] > cb)
                                                if (p[offsets[3]] > cb)
                                                    if (p[offsets[2]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[10]] > cb)
                                                            if (p[offsets[11]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[10]] > cb)
                                                        if (p[offsets[11]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[10]] > cb)
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[10]] > cb)
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[13]] > cb)
                                                            if (p[offsets[14]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[10]] > cb)
                                            if (p[offsets[11]] > cb)
                                                if (p[offsets[12]] > cb)
                                                    if (p[offsets[13]] > cb)
                                                        if (p[offsets[14]] > cb)
                                                            if (p[offsets[15]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else if (p[offsets[8]] < c_b)
                            if (p[offsets[9]] < c_b)
                                if (p[offsets[10]] < c_b)
                                    if (p[offsets[11]] < c_b)
                                        if (p[offsets[12]] < c_b)
                                            if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[3]] < c_b)
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[2]] < c_b)
                                            if (p[offsets[3]] < c_b)
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else
                            goto is_not_a_corner;
                    else if (p[offsets[1]] < c_b)
                        if (p[offsets[2]] > cb)
                            if (p[offsets[9]] > cb)
                                if (p[offsets[7]] > cb)
                                    if (p[offsets[8]] > cb)
                                        if (p[offsets[10]] > cb)
                                            if (p[offsets[6]] > cb)
                                                if (p[offsets[5]] > cb)
                                                    if (p[offsets[4]] > cb)
                                                        if (p[offsets[3]] > cb)
                                                            goto is_a_corner;
                                                        else
                                                            if (p[offsets[11]] > cb)
                                                                if (p[offsets[12]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        if (p[offsets[11]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[14]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[13]] > cb)
                                                            if (p[offsets[14]] > cb)
                                                                if (p[offsets[15]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else if (p[offsets[9]] < c_b)
                                if (p[offsets[10]] < c_b)
                                    if (p[offsets[11]] < c_b)
                                        if (p[offsets[12]] < c_b)
                                            if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[3]] < c_b)
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else if (p[offsets[2]] < c_b)
                            if (p[offsets[3]] > cb)
                                if (p[offsets[10]] > cb)
                                    if (p[offsets[7]] > cb)
                                        if (p[offsets[8]] > cb)
                                            if (p[offsets[9]] > cb)
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[6]] > cb)
                                                        if (p[offsets[5]] > cb)
                                                            if (p[offsets[4]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                if (p[offsets[12]] > cb)
                                                                    if (p[offsets[13]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                        else
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                    if (p[offsets[14]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[14]] > cb)
                                                                    if (p[offsets[15]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else if (p[offsets[10]] < c_b)
                                    if (p[offsets[11]] < c_b)
                                        if (p[offsets[12]] < c_b)
                                            if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else if (p[offsets[3]] < c_b)
                                if (p[offsets[4]] > cb)
                                    if (p[offsets[13]] > cb)
                                        if (p[offsets[7]] > cb)
                                            if (p[offsets[8]] > cb)
                                                if (p[offsets[9]] > cb)
                                                    if (p[offsets[10]] > cb)
                                                        if (p[offsets[11]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[6]] > cb)
                                                                    if (p[offsets[5]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        if (p[offsets[14]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                else
                                                                    if (p[offsets[14]] > cb)
                                                                        if (p[offsets[15]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else if (p[offsets[13]] < c_b)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[5]] > cb)
                                                if (p[offsets[6]] > cb)
                                                    if (p[offsets[7]] > cb)
                                                        if (p[offsets[8]] > cb)
                                                            if (p[offsets[9]] > cb)
                                                                if (p[offsets[10]] > cb)
                                                                    if (p[offsets[12]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        if (p[offsets[10]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        if (p[offsets[10]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        if (p[offsets[5]] > cb)
                                            if (p[offsets[6]] > cb)
                                                if (p[offsets[7]] > cb)
                                                    if (p[offsets[8]] > cb)
                                                        if (p[offsets[9]] > cb)
                                                            if (p[offsets[10]] > cb)
                                                                if (p[offsets[11]] > cb)
                                                                    if (p[offsets[12]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else if (p[offsets[4]] < c_b)
                                    if (p[offsets[5]] > cb)
                                        if (p[offsets[14]] > cb)
                                            if (p[offsets[7]] > cb)
                                                if (p[offsets[8]] > cb)
                                                    if (p[offsets[9]] > cb)
                                                        if (p[offsets[10]] > cb)
                                                            if (p[offsets[11]] > cb)
                                                                if (p[offsets[12]] > cb)
                                                                    if (p[offsets[13]] > cb)
                                                                        if (p[offsets[6]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            if (p[offsets[15]] > cb)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else if (p[offsets[14]] < c_b)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[6]] > cb)
                                                    if (p[offsets[7]] > cb)
                                                        if (p[offsets[8]] > cb)
                                                            if (p[offsets[9]] > cb)
                                                                if (p[offsets[10]] > cb)
                                                                    if (p[offsets[11]] > cb)
                                                                        if (p[offsets[13]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        if (p[offsets[10]] < c_b)
                                                                            if (p[offsets[11]] < c_b)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            if (p[offsets[6]] > cb)
                                                if (p[offsets[7]] > cb)
                                                    if (p[offsets[8]] > cb)
                                                        if (p[offsets[9]] > cb)
                                                            if (p[offsets[10]] > cb)
                                                                if (p[offsets[11]] > cb)
                                                                    if (p[offsets[12]] > cb)
                                                                        if (p[offsets[13]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else if (p[offsets[5]] < c_b)
                                        if (p[offsets[6]] > cb)
                                            if (p[offsets[15]] < c_b)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[7]] > cb)
                                                        if (p[offsets[8]] > cb)
                                                            if (p[offsets[9]] > cb)
                                                                if (p[offsets[10]] > cb)
                                                                    if (p[offsets[11]] > cb)
                                                                        if (p[offsets[12]] > cb)
                                                                            if (p[offsets[14]] > cb)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                if (p[offsets[7]] > cb)
                                                    if (p[offsets[8]] > cb)
                                                        if (p[offsets[9]] > cb)
                                                            if (p[offsets[10]] > cb)
                                                                if (p[offsets[11]] > cb)
                                                                    if (p[offsets[12]] > cb)
                                                                        if (p[offsets[13]] > cb)
                                                                            if (p[offsets[14]] > cb)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else if (p[offsets[6]] < c_b)
                                            if (p[offsets[7]] > cb)
                                                if (p[offsets[14]] > cb)
                                                    if (p[offsets[8]] > cb)
                                                        if (p[offsets[9]] > cb)
                                                            if (p[offsets[10]] > cb)
                                                                if (p[offsets[11]] > cb)
                                                                    if (p[offsets[12]] > cb)
                                                                        if (p[offsets[13]] > cb)
                                                                            if (p[offsets[15]] > cb)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else if (p[offsets[7]] < c_b)
                                                if (p[offsets[8]] < c_b)
                                                    goto is_a_corner;
                                                else
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[13]] > cb)
                                                if (p[offsets[7]] > cb)
                                                    if (p[offsets[8]] > cb)
                                                        if (p[offsets[9]] > cb)
                                                            if (p[offsets[10]] > cb)
                                                                if (p[offsets[11]] > cb)
                                                                    if (p[offsets[12]] > cb)
                                                                        if (p[offsets[14]] > cb)
                                                                            if (p[offsets[15]] > cb)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[12]] > cb)
                                            if (p[offsets[7]] > cb)
                                                if (p[offsets[8]] > cb)
                                                    if (p[offsets[9]] > cb)
                                                        if (p[offsets[10]] > cb)
                                                            if (p[offsets[11]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                    if (p[offsets[14]] > cb)
                                                                        if (p[offsets[6]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            if (p[offsets[15]] > cb)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else if (p[offsets[12]] < c_b)
                                            if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        if (p[offsets[10]] < c_b)
                                                                            if (p[offsets[11]] < c_b)
                                                                                goto is_a_corner;
                                                                            else
                                                                                goto is_not_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    if (p[offsets[11]] > cb)
                                        if (p[offsets[7]] > cb)
                                            if (p[offsets[8]] > cb)
                                                if (p[offsets[9]] > cb)
                                                    if (p[offsets[10]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[6]] > cb)
                                                                    if (p[offsets[5]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        if (p[offsets[14]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                else
                                                                    if (p[offsets[14]] > cb)
                                                                        if (p[offsets[15]] > cb)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else if (p[offsets[11]] < c_b)
                                        if (p[offsets[12]] < c_b)
                                            if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        if (p[offsets[10]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        if (p[offsets[10]] < c_b)
                                                                            goto is_a_corner;
                                                                        else
                                                                            goto is_not_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                            else
                                if (p[offsets[10]] > cb)
                                    if (p[offsets[7]] > cb)
                                        if (p[offsets[8]] > cb)
                                            if (p[offsets[9]] > cb)
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[6]] > cb)
                                                            if (p[offsets[5]] > cb)
                                                                if (p[offsets[4]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    if (p[offsets[13]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                            else
                                                                if (p[offsets[13]] > cb)
                                                                    if (p[offsets[14]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                        else
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[14]] > cb)
                                                                    if (p[offsets[15]] > cb)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else if (p[offsets[10]] < c_b)
                                    if (p[offsets[11]] < c_b)
                                        if (p[offsets[12]] < c_b)
                                            if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    if (p[offsets[9]] < c_b)
                                                                        goto is_a_corner;
                                                                    else
                                                                        goto is_not_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                        else
                            if (p[offsets[9]] > cb)
                                if (p[offsets[7]] > cb)
                                    if (p[offsets[8]] > cb)
                                        if (p[offsets[10]] > cb)
                                            if (p[offsets[11]] > cb)
                                                if (p[offsets[6]] > cb)
                                                    if (p[offsets[5]] > cb)
                                                        if (p[offsets[4]] > cb)
                                                            if (p[offsets[3]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                if (p[offsets[12]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                        else
                                                            if (p[offsets[12]] > cb)
                                                                if (p[offsets[13]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                if (p[offsets[14]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[13]] > cb)
                                                            if (p[offsets[14]] > cb)
                                                                if (p[offsets[15]] > cb)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else if (p[offsets[9]] < c_b)
                                if (p[offsets[10]] < c_b)
                                    if (p[offsets[11]] < c_b)
                                        if (p[offsets[12]] < c_b)
                                            if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[3]] < c_b)
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                if (p[offsets[8]] < c_b)
                                                                    goto is_a_corner;
                                                                else
                                                                    goto is_not_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                    else
                        if (p[offsets[8]] > cb)
                            if (p[offsets[7]] > cb)
                                if (p[offsets[9]] > cb)
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[6]] > cb)
                                            if (p[offsets[5]] > cb)
                                                if (p[offsets[4]] > cb)
                                                    if (p[offsets[3]] > cb)
                                                        if (p[offsets[2]] > cb)
                                                            goto is_a_corner;
                                                        else
                                                            if (p[offsets[11]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                    else
                                                        if (p[offsets[11]] > cb)
                                                            if (p[offsets[12]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            if (p[offsets[13]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[13]] > cb)
                                                            if (p[offsets[14]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[11]] > cb)
                                                if (p[offsets[12]] > cb)
                                                    if (p[offsets[13]] > cb)
                                                        if (p[offsets[14]] > cb)
                                                            if (p[offsets[15]] > cb)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else if (p[offsets[8]] < c_b)
                            if (p[offsets[9]] < c_b)
                                if (p[offsets[10]] < c_b)
                                    if (p[offsets[11]] < c_b)
                                        if (p[offsets[12]] < c_b)
                                            if (p[offsets[13]] < c_b)
                                                if (p[offsets[14]] < c_b)
                                                    if (p[offsets[15]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[3]] < c_b)
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[2]] < c_b)
                                            if (p[offsets[3]] < c_b)
                                                if (p[offsets[4]] < c_b)
                                                    if (p[offsets[5]] < c_b)
                                                        if (p[offsets[6]] < c_b)
                                                            if (p[offsets[7]] < c_b)
                                                                goto is_a_corner;
                                                            else
                                                                goto is_not_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else
                            goto is_not_a_corner;
                else
                    if (p[offsets[7]] > cb)
                        if (p[offsets[8]] > cb)
                            if (p[offsets[9]] > cb)
                                if (p[offsets[6]] > cb)
                                    if (p[offsets[5]] > cb)
                                        if (p[offsets[4]] > cb)
                                            if (p[offsets[3]] > cb)
                                                if (p[offsets[2]] > cb)
                                                    if (p[offsets[1]] > cb)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[10]] > cb)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[10]] > cb)
                                                        if (p[offsets[11]] > cb)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[10]] > cb)
                                                    if (p[offsets[11]] > cb)
                                                        if (p[offsets[12]] > cb)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[10]] > cb)
                                                if (p[offsets[11]] > cb)
                                                    if (p[offsets[12]] > cb)
                                                        if (p[offsets[13]] > cb)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[10]] > cb)
                                            if (p[offsets[11]] > cb)
                                                if (p[offsets[12]] > cb)
                                                    if (p[offsets[13]] > cb)
                                                        if (p[offsets[14]] > cb)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    if (p[offsets[10]] > cb)
                                        if (p[offsets[11]] > cb)
                                            if (p[offsets[12]] > cb)
                                                if (p[offsets[13]] > cb)
                                                    if (p[offsets[14]] > cb)
                                                        if (p[offsets[15]] > cb)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else
                            goto is_not_a_corner;
                    else if (p[offsets[7]] < c_b)
                        if (p[offsets[8]] < c_b)
                            if (p[offsets[9]] < c_b)
                                if (p[offsets[6]] < c_b)
                                    if (p[offsets[5]] < c_b)
                                        if (p[offsets[4]] < c_b)
                                            if (p[offsets[3]] < c_b)
                                                if (p[offsets[2]] < c_b)
                                                    if (p[offsets[1]] < c_b)
                                                        goto is_a_corner;
                                                    else
                                                        if (p[offsets[10]] < c_b)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                else
                                                    if (p[offsets[10]] < c_b)
                                                        if (p[offsets[11]] < c_b)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                            else
                                                if (p[offsets[10]] < c_b)
                                                    if (p[offsets[11]] < c_b)
                                                        if (p[offsets[12]] < c_b)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                        else
                                            if (p[offsets[10]] < c_b)
                                                if (p[offsets[11]] < c_b)
                                                    if (p[offsets[12]] < c_b)
                                                        if (p[offsets[13]] < c_b)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                    else
                                        if (p[offsets[10]] < c_b)
                                            if (p[offsets[11]] < c_b)
                                                if (p[offsets[12]] < c_b)
                                                    if (p[offsets[13]] < c_b)
                                                        if (p[offsets[14]] < c_b)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                else
                                    if (p[offsets[10]] < c_b)
                                        if (p[offsets[11]] < c_b)
                                            if (p[offsets[12]] < c_b)
                                                if (p[offsets[13]] < c_b)
                                                    if (p[offsets[14]] < c_b)
                                                        if (p[offsets[15]] < c_b)
                                                            goto is_a_corner;
                                                        else
                                                            goto is_not_a_corner;
                                                    else
                                                        goto is_not_a_corner;
                                                else
                                                    goto is_not_a_corner;
                                            else
                                                goto is_not_a_corner;
                                        else
                                            goto is_not_a_corner;
                                    else
                                        goto is_not_a_corner;
                            else
                                goto is_not_a_corner;
                        else
                            goto is_not_a_corner;
                    else
                        goto is_not_a_corner;

                #endregion

            is_a_corner:
                bmin = b;
                goto end_if;

            is_not_a_corner:
                bmax = b;
                goto end_if;

            end_if:

                if (bmin == bmax - 1 || bmin == bmax)
                    return bmin;

                b = (bmin + bmax) / 2;
            }
        }

        private static int[] makeOffsets(int stride)
        {
            int[] pixel = new int[16];
            pixel[00] = +0 + stride * +3;
            pixel[01] = +1 + stride * +3;
            pixel[02] = +2 + stride * +2;
            pixel[03] = +3 + stride * +1;
            pixel[04] = +3 + stride * +0;
            pixel[05] = +3 + stride * -1;
            pixel[06] = +2 + stride * -2;
            pixel[07] = +1 + stride * -3;
            pixel[08] = +0 + stride * -3;
            pixel[09] = -1 + stride * -3;
            pixel[10] = -2 + stride * -2;
            pixel[11] = -3 + stride * -1;
            pixel[12] = -3 + stride * +0;
            pixel[13] = -3 + stride * +1;
            pixel[14] = -2 + stride * +2;
            pixel[15] = -1 + stride * +3;
            return pixel;
        }
        #endregion
    }
}
