using System.Numerics;

namespace XrEngine
{

    public class PerspectiveCamera : Camera
    {
        public PerspectiveCamera()
        {
        }

        public void SetFovCenter(float left, float right, float top, float bottom)
        {
            Projection = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, Near, Far);
        }

        public void SetFov(float angleDegree, uint width, uint height)
        {
            SetFov(angleDegree, (float)width / height);
        }

        public void SetFov(float angleDegree, float ratio)
        {
            var rads = MathF.PI / 180f * angleDegree;
            Projection = Matrix4x4.CreatePerspectiveFieldOfView(rads, ratio, Near, Far);
        }

    }
}
