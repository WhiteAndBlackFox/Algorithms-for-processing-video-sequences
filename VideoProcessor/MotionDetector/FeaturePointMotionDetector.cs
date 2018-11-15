using System.Collections.Generic;
using AForge;
using VideoProcessor.Features.Base;
using VideoProcessor.Features.FeaturesDetector;
using VideoProcessor.Features.Matching;
using VideoProcessor.Model;

namespace VideoProcessor.MotionDetector
{
    public class FeaturePointMotionDetector
    {
        private readonly SpeededUpRobustFeaturesDetector _surf;
        private readonly KNearestNeighborMatching _matcher;
        private IFeaturePoint<double[]>[] _prevPoints;

        public FeaturePointMotionDetector()
        {
            _surf = new SpeededUpRobustFeaturesDetector();
            _matcher = new KNearestNeighborMatching(5);
            _prevPoints = null;
        }

        public DetectorResult Process(Frame frame, Frame prevFrame)
        {
            if (_prevPoints == null)
            {
                _prevPoints = _surf.ProcessImage(frame.Image).ToArray();
                return null;
            }
            IFeaturePoint<double[]>[] points = _surf.ProcessImage(frame.Image).ToArray();

            var matches = _matcher.Match(_prevPoints, points);

            FeatureDetectorResult detectorResult = new FeatureDetectorResult();
            var len = matches[0].Length;
            detectorResult.FeaturePointCount = points.Length;
            int count = 0;
            for (int i = 0; i < len; i++)
            {
                if (matches[1][i].DistanceTo(matches[0][i]) > 4)
                {
                    detectorResult.Add(matches[1][i].X, matches[1][i].Y);
                    count++;
                }
            }
            detectorResult.SimilarPointCount = count;
            return detectorResult;
        }
    }
}