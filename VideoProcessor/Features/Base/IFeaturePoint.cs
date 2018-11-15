namespace VideoProcessor.Features.Base
{
    public interface IFeaturePoint : IFeaturePoint<double[]>
    {

    }

    public interface IFeaturePoint<out T> : IFeatureDescriptor<T>
    {
        double X { get; }

        double Y { get; }
    }
}
