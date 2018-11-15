namespace VideoProcessor.Features.Base
{
    public interface IFeatureDescriptor<out T>
    {
        T Descriptor { get; }
    }
}
