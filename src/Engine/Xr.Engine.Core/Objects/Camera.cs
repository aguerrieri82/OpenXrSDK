using System.Numerics;

namespace Xr.Engine
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
