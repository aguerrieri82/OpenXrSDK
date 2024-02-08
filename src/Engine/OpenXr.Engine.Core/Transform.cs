using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Transform
    {
        protected bool _isDirty = true;
        protected Vector3 _scale;
        protected Vector3 _position;
        protected Quaternion _orientation;
        protected Matrix4x4 _matrix;

        public Transform()
        {
            _scale = new Vector3(1, 1, 1);
        }

        public bool Update()
        {
            if (_isDirty) 
                return false;

            _matrix = Matrix4x4.Identity
                * Matrix4x4.CreateTranslation(_position)
                * Matrix4x4.CreateFromQuaternion(_orientation)
                * Matrix4x4.CreateScale(_scale);

            _isDirty = false;
            return true;
        }

        public Vector3 Scale
        {
            get => _scale;

            set {
                _scale = value;
                _isDirty = true;
            }
        }

        public Quaternion Orientation
        {
            get => _orientation;

            set
            {
                _orientation = value;
                _isDirty = true;
            }
        }

        public Vector3 Position
        {
            get => _position;

            set
            {
                _position = value;
                _isDirty = true;
            }
        }

        public ref Matrix4x4 Matrix => ref _matrix;
    }
}
