using System.Numerics;
using XrMath;

namespace XrEngine
{

    public class PerspectiveCamera : Camera
    {
        protected float _fovDeg;

        public PerspectiveCamera()
            : this(true)
        {
        }

        public PerspectiveCamera(bool isInit)
            : base(isInit)
        {
        }

        public void SetFov(float left, float right, float top, float bottom)
        {
            Projection = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, _near, _far);
        }

        public void SetFov(float fovDeg, uint width, uint height)
        {
            ViewSize = new Size2I
            {
                Width = width,
                Height = height
            };

            FovDegree = fovDeg;
        }

        /*
        protected Vector2 GetFovDegrees()
        {
            return new Vector2
            {
                X = (2.0f * MathF.Atan(1.0f / _proj.M22)).ToDegrees(),
                Y = (2.0f * MathF.Atan(1.0f / _proj.M11)).ToDegrees()
            };
        }
        */

        protected override void BuildProjection()
        {
            if (_fovDeg > 0 && ViewSize.Width > 0 && ViewSize.Height > 0 && _far != 0 && _near != 0)
            {
                _proj = Matrix4x4.CreatePerspectiveFieldOfView(
                    _fovDeg.ToRadians(),
                    (float)ViewSize.Width / ViewSize.Height, 
                    _near, _far);

                _projInverseDirty = true;
                _viewProjDirty = true;
                _projDirty = false;
            }
        }

        public override void CopyFrom(Camera camera)
        {
            if (camera is PerspectiveCamera persp)
            {
                _fovDeg = persp._fovDeg;
            }

            base.CopyFrom(camera);
        }

        protected override void ExtractProjectionInternals()
        {
            var fov = ((float)(2.0 * Math.Atan(1.0 / _proj.M22))).ToDegrees();

            var near = _proj.M43 / (_proj.M33 - 1.0f);

            float far;
            if (MathF.Abs(_proj.M33 + 1.0f) < 0.000001f)
                far = float.PositiveInfinity;
            else
                far = _proj.M43 / (_proj.M33 + 1.0f);

            if (MathF.Abs(fov - _fovDeg) > 0.001f)
                _fovDeg = fov;

            if (MathF.Abs(near - _near) > 0.001f)
                _near = near;

            if (float.IsInfinity(far) || MathF.Abs(far - _far) > 0.1f)
                _far = far;
        }
        
        [Range(0, 180, 1)]
        public float FovDegree
        {
            get => _fovDeg;
            set
            {
                if (_fovDeg == value)
                    return;
                _fovDeg = value;

                _projDirty = true;
                _viewProjDirty = true;
            }
        }
    }

}
