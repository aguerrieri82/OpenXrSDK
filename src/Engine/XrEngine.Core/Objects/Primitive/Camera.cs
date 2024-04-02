using System.Numerics;
using XrMath;

namespace XrEngine
{

    public abstract class Camera : Object3D
    {
        private Matrix4x4 _projInverse;
        private Matrix4x4 _proj;

        public Camera()
        {
            Near = 0.01f;
            Far = 10;
            Exposure = 1;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(BackgroundColor), BackgroundColor);
            container.Write(nameof(Near), Near);
            container.Write(nameof(Far), Far);
            container.Write(nameof(Exposure), Exposure);
            container.Write(nameof(Projection), Projection);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            BackgroundColor = container.Read<Color>(nameof(BackgroundColor));
            Near = container.Read<float>(nameof(Near));
            Far = container.Read<float>(nameof(Far));
            Exposure = container.Read<float>(nameof(Exposure));
            Projection = container.Read<Matrix4x4>(nameof(Projection));
        }


        public Color BackgroundColor { get; set; }

        public float Near { get; set; }

        public float Far { get; set; }

        public float Exposure { get; set; }

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

    }
}
