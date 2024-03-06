using System.Numerics;

namespace Xr.Engine
{
    public class Transform3
    {
        protected bool _isDirty;
        protected Vector3 _scale;
        protected Vector3 _position;
        protected Quaternion _orientation;
        protected Matrix4x4 _matrix;
        protected Vector3 _localPivot;
        protected Vector3 _rotation;
        protected Object3D? _host;

        public Transform3(Object3D? host = null)
        {
            _host = host;
            _scale = new Vector3(1, 1, 1);
            _orientation = Quaternion.Identity;
            _matrix = Matrix4x4.Identity;
        }

        public Transform3 Clone()
        {
            return new Transform3
            {
                LocalPivot = _localPivot,
                Position = _position,
                Orientation = _orientation,
                Scale = _scale,
            };
        }

        public bool Update(bool force = false)
        {
            if (!_isDirty && !force)
                return false;

            _matrix =
                Matrix4x4.CreateTranslation(-_localPivot) *
                Matrix4x4.CreateScale(_scale) *
                Matrix4x4.CreateFromQuaternion(_orientation) *
                Matrix4x4.CreateTranslation(_position);


            _isDirty = false;

            _host?.NotifyChanged(ObjectChangeType.Transform);

            return true;
        }

        public void SetLocalPivot(Vector3 value, bool keepPosition)
        {
            if (keepPosition)
            {
                _position += (value - _localPivot).Transform(
                    Matrix4x4.CreateScale(_scale) * Matrix4x4.CreateFromQuaternion(_orientation));
            }
            LocalPivot = value;
        }

        protected void NotifyChanged()
        {
            _isDirty = true;
        }

        public Vector3 Scale
        {
            get => _scale;

            set
            {
                _scale = value;
                NotifyChanged();
            }
        }

        public Quaternion Orientation
        {
            get => _orientation;

            set
            {
                _orientation = Quaternion.Normalize(value);
                _rotation = _orientation.ToEuler();
                NotifyChanged();
            }
        }

        public Vector3 Position
        {
            get => _position;

            set
            {
                _position = value;
                NotifyChanged();
            }
        }

        public Vector3 LocalPivot
        {
            get => _localPivot;
            set
            {
                _localPivot = value;
                NotifyChanged();
            }
        }

        public Vector3 Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                _orientation = Quaternion.CreateFromYawPitchRoll(value.Y, value.X, value.Z);
                NotifyChanged();
            }
        }

        public void SetMatrix(Matrix4x4 matrix)
        {
            _matrix = matrix;

            Matrix4x4.Decompose(matrix, out _scale, out _orientation, out _position);

            _rotation = _orientation.ToEuler();

            _host?.NotifyChanged(ObjectChangeType.Transform);
        }

        public ref Matrix4x4 Matrix
        {
            get
            {
                if (_isDirty)
                    Update();
                return ref _matrix;
            }
        }
    }
}
