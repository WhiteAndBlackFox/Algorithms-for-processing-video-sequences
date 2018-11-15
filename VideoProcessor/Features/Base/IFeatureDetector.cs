using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;

namespace VideoProcessor.Features.Base
{
    public interface IFeatureDetector<TPoint> : IFeatureDetector<TPoint, double[]>
        where TPoint : IFeatureDescriptor<double[]>
    {

    }

    public interface IFeatureDetector<TPoint, TFeature> 
        where TPoint : IFeatureDescriptor<TFeature>
    {
        List<TPoint> ProcessImage(Bitmap image);

        List<TPoint> ProcessImage(BitmapData imageData);

        List<TPoint> ProcessImage(UnmanagedImage image);
    }

}
