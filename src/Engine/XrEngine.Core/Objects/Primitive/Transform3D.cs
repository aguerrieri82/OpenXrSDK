﻿using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Transform3D : IStateManager
    {
        protected bool _isDirty;
        protected Vector3 _scale;
        protected Vector3 _position;
        protected Quaternion _orientation;
        protected Matrix4x4 _matrix;
        protected Vector3 _localPivot;
        protected Vector3 _rotation;
        protected Object3D? _host;

        public Transform3D(Object3D? host = null)
        {
            _host = host;
            _scale = new Vector3(1, 1, 1);
            _orientation = Quaternion.Identity;
            _matrix = Matrix4x4.Identity;
        }

        public Transform3D Clone()
        {
            return new Transform3D
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

            return true;
        }

        public void SetLocalPivot(Vector3 value, bool keepPosition)
        {
            if (keepPosition)
            {
                var localPosAdjusted = (value - _localPivot) * _scale;
                _position += localPosAdjusted.Transform(_orientation);
            }
            LocalPivot = value;
        }

        protected void NotifyChanged()
        {
            _isDirty = true;

            _host?.NotifyChanged(ObjectChangeType.Transform);

            Version++;
        }

        public Vector3 Scale
        {
            get => _scale;

            set
            {
                if (_scale.IsSimilar(value, SCALE_TOLLERANCE))
                    return;
                _scale = value;
                NotifyChanged();
            }
        }

        public Quaternion Orientation
        {
            get => _orientation;

            set
            {
                if (value.W == 0)
                    value.W = 1;
                value = Quaternion.Normalize(value);
                if (_orientation.IsSimilar(value, ORIENTATION_TOLLERANCE))
                    return;
                _orientation = value;
                _rotation = _orientation.ToEuler();
                NotifyChanged();
            }
        }

        public Vector3 Position
        {
            get => _position;

            set
            {
                if (_position.IsSimilar(value, POS_TOLLERANCE))
                    return;
                _position = value;
                NotifyChanged();
            }
        }

        public Vector3 LocalPivot
        {
            get => _localPivot;
            set
            {
                if (_localPivot.IsSimilar(value, POS_TOLLERANCE))
                    return;
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
                //_orientation = Quaternion.CreateFromYawPitchRoll(value.Y, value.X, value.Z);
                _orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, value.Z) *
                               Quaternion.CreateFromAxisAngle(Vector3.UnitX, value.X) *
                               Quaternion.CreateFromAxisAngle(Vector3.UnitY, value.Y);
                NotifyChanged();
            }
        }

        public void Set(Transform3D other)
        {
            _scale = other.Scale;
            _localPivot = other.LocalPivot;
            _position = other.Position;
            _orientation = other.Orientation;
            _rotation = _orientation.ToEuler();
            NotifyChanged();
        }

        public void Set(Matrix4x4 matrix)
        {
            if (matrix == _matrix)
                return;

            matrix.DecomposeDouble(out _scale, out _orientation, out _position);

            _rotation = _orientation.ToEuler();
            _matrix = matrix;

            _host?.NotifyChanged(ObjectChangeType.Transform);
            Version++;
        }

        public void Reset()
        {
            SetLocalPivot(Vector3.Zero, true);
            Set(Matrix4x4.Identity);
        }

        public void GetState(IStateContainer container)
        {
            container.Write(nameof(Scale), _scale);
            container.Write(nameof(Position), _position);
            container.Write(nameof(Orientation), _orientation);
            container.Write(nameof(LocalPivot), _localPivot);
        }

        public void SetState(IStateContainer container)
        {
            _scale = container.Read<Vector3>(nameof(Scale));
            _position = container.Read<Vector3>(nameof(Position));
            _orientation = container.Read<Quaternion>(nameof(Orientation));
            _localPivot = container.Read<Vector3>(nameof(LocalPivot));
            _isDirty = true;
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

        public long Version { get; set; }

        public Object3D Host => _host!;


        public const float POS_TOLLERANCE = 0.0001f;
        public const float SCALE_TOLLERANCE = 0.00001f;
        public const float ORIENTATION_TOLLERANCE = 0.0001f;
    }
}
