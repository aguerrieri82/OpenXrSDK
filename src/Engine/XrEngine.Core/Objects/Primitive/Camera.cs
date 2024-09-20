using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct CameraEye
    {
        public Matrix4x4 Transform;

        public Matrix4x4 Projection;
    }

    public abstract class Camera : Object3D
    {
        private Matrix4x4 _projInverse;
        private Matrix4x4 _proj;
        protected Vector3 _target;


        public Camera()
        {
            Near = 0.01f;
            Far = 10;
            Exposure = 1;
        }

        public void LookAt(Vector3 position, Vector3 target, Vector3 up)
        {
            View = Matrix4x4.CreateLookAt(position, target, up);
            _target = target;
        }


        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(BackgroundColor), BackgroundColor);
            container.Write(nameof(Near), Near);
            container.Write(nameof(Far), Far);
            container.Write(nameof(Exposure), Exposure);
            container.Write(nameof(Projection), Projection);
            container.Write(nameof(Target), Target);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            BackgroundColor = container.Read<Color>(nameof(BackgroundColor));
            Near = container.Read<float>(nameof(Near));
            Far = container.Read<float>(nameof(Far));
            Exposure = container.Read<float>(nameof(Exposure));
            Projection = container.Read<Matrix4x4>(nameof(Projection));
            Target = container.Read<Vector3>(nameof(Target));
        }

        public Camera Clone()
        {
            var camera = (Camera)Activator.CreateInstance(GetType())!;
            camera.CopyFrom(this);
            return camera;
        }

        protected virtual void CopyFrom(Camera camera)
        {
            BackgroundColor = camera.BackgroundColor;
            Near = camera.Near;
            Far = camera.Far;
            Exposure = camera.Exposure;
            Projection = camera.Projection;
            WorldMatrix = camera.WorldMatrix;
            Target = camera.Target;
            Eyes = camera.Eyes;
            ActiveEye = camera.ActiveEye;
        }


        public Vector3 Target
        {
            get => _target;
            set
            {
                LookAt(WorldPosition, value, Up);
            }
        }

        public Matrix4x4 View
        {
            get => WorldMatrixInverse;
            set
            {
                Matrix4x4.Invert(value, out var inverse);
                WorldMatrix = inverse;
            }
        }

        public Matrix4x4 Projection
        {
            get => _proj;
            set
            {
                _proj = value;
                Matrix4x4.Invert(_proj, out _projInverse);
            }
        }

        public Matrix4x4 ProjectionInverse => _projInverse;

        public Matrix4x4 ViewInverse => WorldMatrix;

        public Color BackgroundColor { get; set; }

        public float Near { get; set; }

        public float Far { get; set; }

        public float Exposure { get; set; }

        public CameraEye[]? Eyes { get; set; }

        public int ActiveEye { get; set; }


    }
}
