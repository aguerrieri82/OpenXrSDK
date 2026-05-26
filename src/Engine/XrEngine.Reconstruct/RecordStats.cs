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

        public int DepthFrame { get; set; }

        public int RightFrame { get; set; }

        public int ScreenFrame { get; set; }

        public int LeftFrame { get; set; }
    }
}
