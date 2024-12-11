using XrMath;

namespace XrEngine.OpenXr
{
    public interface IPosePredictor
    {
        Pose3 Predict(float dt);

        void Track(Pose3 pose, float time);
    }
}
