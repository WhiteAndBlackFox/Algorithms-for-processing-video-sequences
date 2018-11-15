using System;
using VideoProcessor.Model;

namespace VideoProcessor.MotionDetector {
    public class SceneChangeDetector
    {
        private double _lastAvgBrighness;
        private int _sceneNumber;

        public SceneChangeDetector()
        {
            _lastAvgBrighness = 1000;
        }

        public void Process(Frame frame, int threshold, Action<int> onNewScene)
        {
            var averageBrightness = frame.GetAverageBrightness();
            if (Math.Abs(averageBrightness - _lastAvgBrighness) > threshold)
            {
                _sceneNumber++;
            }
            _lastAvgBrighness = averageBrightness;
            onNewScene(_sceneNumber);
        }
    }
}
