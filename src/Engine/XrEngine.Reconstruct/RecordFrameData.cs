using XrEngine.Devices;
using XrMath;

namespace XrEngine.Reconstruct
{
    public class EyeData
    {
        public float[]? Proj { get; set; }

        public float[]? View { get; set; }

        public long Time { get; set; }

        public Pose3? Pose { get; set; }

        public CameraParams? CameraParams { get; set; }
    }

    public class RecordFrameData
    {
        public int Frame { get; set; }

        public long Time { get; set; }

        public EyeData? LeftColor { get; set; }

        public EyeData? RightColor { get; set; }

        public EyeData? LeftDepth { get; set; }

        public EyeData? RightDepth { get; set; }
    }
}
