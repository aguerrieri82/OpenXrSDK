using System.Numerics;
using XrMath;

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
            ViewSize = new Size2I
            {
                Width = width,
                Height = height
            };

            SetFov(angleDegree, (float)width / height);
        }

        public void SetFov(float angleDegree, float ratio)
        {
            FovDegree = angleDegree;
            var rads = MathF.PI / 180f * angleDegree;
            Projection = Matrix4x4.CreatePerspectiveFieldOfView(rads, ratio, Near, Far);
        }

        public void UpdateProjection()
        {
            SetFov(FovDegree, ViewSize.Width, ViewSize.Height);
        }

        public float FovDegree { get; set; }
    }

}
