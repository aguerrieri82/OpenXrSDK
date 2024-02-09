
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
        protected bool _worldDirty;
        protected Group? _scene;
        protected bool _isVisible;

        public Object3D()
        {
            _transform = new Transform();
            _worldDirty = true;
            IsVisible = true;
        }

        public virtual bool UpdateWorldMatrix(bool updateChildren, bool updateParent)
        {
            bool isParentChanged = false;
            bool isChanged = false;

            if (updateParent && _parent != null)
                isParentChanged = _parent.UpdateWorldMatrix(false, updateParent);
            
            if (_transform.Update() || isParentChanged || _worldDirty)
            {
                if (_parent != null)
                    _worldMatrix = _parent!.WorldMatrix * _transform.Matrix;
                else
                    _worldMatrix = _transform.Matrix;

                _worldDirty = false;

                isChanged = true;
            }

            return isChanged;
        }

        public override void Update(RenderContext ctx)
        {
            if (_transform.Update())
            {
                _worldDirty = true;
                UpdateWorldMatrix(true, false);
            }


            base.Update(ctx);
        }

        public Group? Parent
        {
            get => _parent;
            internal set
            {
                _parent = value;
                _worldDirty = true;
                _scene = this.FindAncestor<Scene>();
                NotifyChanged();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
                NotifyChanged();


            }
        }

        public virtual void NotifyChanged()
        {
            if (_scene != null)
                _scene.Version++;
        }

        public Matrix4x4 WorldMatrix => _worldMatrix;

        public Transform Transform => _transform;

        public string? Tag { get; set; }

        public string? Name { get; set; }    
    }
}
