﻿using System.Numerics;

namespace OpenXr.Engine.Objects
{
    public class RayView : Mesh
    {
        protected float _length;
        protected bool _isDirty = true;
        protected float _size;

        public RayView()
        {
            Geometry = new Cube();
            Materials.Add(new ColorMaterial(Color.White));
            Length = 4f;
            Size = 0.005f;

            var matrix = Matrix4x4.CreateScale(0.5f, 0.5f, 0.5f) *
                         Matrix4x4.CreateTranslation(0, 0, -0.5f);

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
