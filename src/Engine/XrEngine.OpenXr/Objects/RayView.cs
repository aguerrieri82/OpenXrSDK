using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class RayView : TriangleMesh
    {
        protected float _length;
        protected bool _isDirty = true;
        protected float _size;

        public RayView()
        {
            Flags |= EngineObjectFlags.DisableNotifyChangedScene;

            Geometry = new Cube3D(Vector3.One);
            Materials.Add(new ColorMaterial(Color.White));
            Length = 4f;
            Size = 0.005f;
            Name = "RayView";

            var matrix = Matrix4x4.CreateTranslation(0, 0, -0.5f);

            Geometry!.ApplyTransform(matrix);
        }

        public override void Update(RenderContext ctx)
        {
            if (_isDirty)
            {
                Transform.SetScale(_size, _size, _length);
                _isDirty = false;
            }

            base.Update(ctx);
        }

        public float Length
        {
            get => _length;
            set
            {
                _length = value;
                _isDirty = true;
            }
        }

        public float Size
        {
            get => _size;
            set
            {
                _size = value;
                _isDirty = true;
            }
        }
    }
}
