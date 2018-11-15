namespace VideoProcessor.Helpers {
    public static class NumberHelper {
        public static string ToTimeString(this uint seconds)
        {
            return string.Format("{0}:{1}", seconds / 60, (seconds % 60).ToString("D2"));
        }
    }
}
