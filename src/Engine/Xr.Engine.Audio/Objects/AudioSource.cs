using Silk.NET.OpenAL;
using System.Numerics;

namespace Xr.Engine.Audio
{
    public class AudioSource : AlObject, IDisposable
    {
        public AudioSource(AL al)
            : base(al, al.GenSource())
        {

        }

        public AudioSource(AL al, uint handle)
            : base(al, handle)
        {

        }

        public void Play()
        {
            _al.SourcePlay(_handle);
        }

        public void Stop()
        {
            _al.SourceStop(_handle);
        }

        public void Pause()
        {
            _al.SourcePause(_handle);
        }

        public void Rewind()
        {
            _al.SourceRewind(_handle);
        }

        public Vector3 Velocity
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceVector3.Velocity, out Vector3 value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceVector3.Velocity, value);
        }

        public Vector3 Position
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceVector3.Position, out Vector3 value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceVector3.Position, value);
        }

        public Vector3 Direction
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceVector3.Direction, out Vector3 value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceVector3.Direction, value);
        }


        public int PlayPositionSamples
        {
            get
            {
                _al.GetSourceProperty(_handle, GetSourceInteger.SampleOffset, out int value);
                return value;
            }
        }

        public SourceState State
        {
            get
            {
                _al.GetSourceProperty(_handle, GetSourceInteger.SourceState, out int type);
                return (SourceState)type;
            }
        }

        public SourceType Type
        {
            get
            {
                _al.GetSourceProperty(_handle, GetSourceInteger.SourceType, out int type);
                return (SourceType)type;
            }
            set => _al.SetSourceProperty(_handle, SourceInteger.SourceType, (int)value);
        }

        public bool IsLooping
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceBoolean.Looping, out bool value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceBoolean.Looping, value);
        }

        public float Pitch
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.Pitch, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.Pitch, value);
        }

        public float Gain
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.Gain, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.Gain, value);
        }


        public float PlayPositionSecs
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.SecOffset, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.SecOffset, value);
        }

        public float MaxDistance
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.MaxDistance, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.MaxDistance, value);
        }

        public float MaxGain
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.MaxGain, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.MaxGain, value);
        }

        public float MinGain
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.MinGain, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.MinGain, value);
        }


        public float ConeInnerAngleDeg
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.ConeInnerAngle, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.ConeInnerAngle, value);
        }

        public float ConeOuterAngleDeg
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.ConeOuterAngle, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.ConeOuterAngle, value);
        }

        public float ReferenceDistance
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.ReferenceDistance, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.ReferenceDistance, value);
        }


        public float RolloffFactor
        {
            get
            {
                _al.GetSourceProperty(_handle, SourceFloat.RolloffFactor, out float value);
                return value;
            }
            set => _al.SetSourceProperty(_handle, SourceFloat.RolloffFactor, value);
        }

        public void Dispose()
        {
            if (_handle != 0)
            {
                _al.DeleteSource(_handle);
                _handle = 0;
            }

            GC.SuppressFinalize(this);
        }
    }

}
