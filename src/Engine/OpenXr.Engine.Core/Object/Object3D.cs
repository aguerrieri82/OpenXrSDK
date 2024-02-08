
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public abstract class Object3D : EngineObject
    {
        protected Transform _transform;
        protected Group? _parent;
        protected Matrix4x4 _worldMatrix;

        public Object3D()
        {
            _transform = new Transform();
            IsVisible = true;
        }

        public virtual bool UpdateWorldMatrix(bool updateChildren, bool updateParent)
        {
            bool isParentChanged = false;
            bool isChanged = false;

            if (updateParent && _parent != null)
                isParentChanged = _parent.UpdateWorldMatrix(false, updateParent);
            
            if (_transform.Update() || isParentChanged)
            {
                _worldMatrix = _parent!.WorldMatrix * _transform.Matrix;
                isChanged = true;
            }

            return isChanged;
        }

        public override void Update(RenderContext ctx)
        {
            _transform.Update();

            base.Update(ctx);
        }

        public Group? Parent
        {
            get => _parent;
            internal set => _parent = value;
        }

        public bool IsVisible { get; set; }

        public Matrix4x4 WorldMatrix => _worldMatrix;

        public Transform Transform => _transform;

        public string? Tag { get; set; }

        public string? Name { get; set; }    
    }
}
