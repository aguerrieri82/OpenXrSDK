using System.Numerics;

namespace OpenXr.Engine
{
    public abstract class Camera : Object3D
    {
        public Camera()
        {
            Near = 0.01f;
            Far = 10;
        }

        public Color BackgroundColor { get; set; }

        public float Near { get; set; }

        public float Far { get; set; }

        public Matrix4x4 Projection { get; set; }
    }
}
