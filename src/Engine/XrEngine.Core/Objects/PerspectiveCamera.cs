using System.Numerics;

namespace XrEngine
{
    public struct CameraEye
    {
        public Matrix4x4 Transform;

        public Matrix4x4 Projection;
    }

    public class PerspectiveCamera : Camera
    {
        protected Matrix4x4 _viewInverse;
        protected Vector3 _target;

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

        public void LookAt(Vector3 position, Vector3 target, Vector3 up)
        {
            View = Matrix4x4.CreateLookAt(position, target, up);
            _target = target;
        }

        public Vector3 Target
        {
            get => _target;
            set
            {
                LookAt(WorldPosition, value, Up);
            }
        }

        public CameraEye[]? Eyes { get; set; }
    }
}
