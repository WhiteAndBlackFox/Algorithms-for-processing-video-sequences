namespace VideoProcessor.Model
{
    public class FeatureDetectorResult : DetectorResult
    {
        public int FeaturePointCount { get; set; }
        public int SimilarPointCount { get; set; }
    }
}