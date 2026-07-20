using XrMath;

namespace XrEngine.Media
{
    public interface ICameraPoseProvider
    {
        Pose3? GetCameraPose(string cameraId, long frameTime);
    }
}
