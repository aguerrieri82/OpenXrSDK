using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
            Length = 5;
            Size = 0.005f;
        }

        public override void Update(RenderContext ctx)
        {
            if (_isDirty)
            {
                _transform.Scale = new Vector3(_size, _size, _length);
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
