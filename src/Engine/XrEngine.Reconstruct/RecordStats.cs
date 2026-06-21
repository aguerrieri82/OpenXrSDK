using XrMath;

namespace XrEngine.Reconstruct
{
    public class RecordStatsImage
    {
        public Pose3 Pose { get; set; }

        public long ImageTime { get; set; }

        public long XrTime { get; set; }

        public long BootTime { get; set; }

        public long NanoTime { get; set; }
    }

    public class RecordStats
    {
        public List<RecordStatsImage> Images { get; set; } = [];

        public Pose3 ScenePosition { get; set; }

        public int DepthFrames { get; set; }

        public int RightFrames { get; set; }

        public int ScreenFrames { get; set; }

        public int LeftFrames { get; set; }
    }
}
