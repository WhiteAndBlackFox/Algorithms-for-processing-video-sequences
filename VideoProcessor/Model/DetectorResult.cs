using System.Collections.Generic;
using System.Linq;

namespace VideoProcessor.Model {
    public class DetectorResult
    {
        public DetectorResult() {
            Regions = new List<DetectorRegion>();
        }

        public List<DetectorRegion> Regions { get; set; }

        public void Add(int x, int y, int minDistance = 40) {
            var region = Regions.FirstOrDefault(p => p.Check(x, y, minDistance));
            if (region != null) {
                region.Add(x, y);
            }
            else {
                Regions.Add(new DetectorRegion(x, y));
            }
        }
    }
}
