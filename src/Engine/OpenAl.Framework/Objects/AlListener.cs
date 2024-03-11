using Silk.NET.OpenAL;
using System.Numerics;

namespace OpenAl.Framework
{
    public struct AudioOrientation
    {
        public Vector3 Forward;

        public Vector3 Up;
    }

    public class AlListener : AlObject
    {
        public AlListener(AL al) : base(al, 0)
        {

        }

        public float Gain
        {
            get
            {
                _al.GetListenerProperty(ListenerFloat.Gain, out float value);
                return value;
            }
            set => _al.SetListenerProperty(ListenerFloat.Gain, value);
        }

        public Vector3 Velocity
        {
            get
            {
                _al.GetListenerProperty(ListenerVector3.Velocity, out Vector3 value);
                return value;
            }
            set => _al.SetListenerProperty(ListenerVector3.Velocity, value);
        }

        public Vector3 Position
        {
            get
            {
                _al.GetListenerProperty(ListenerVector3.Position, out Vector3 value);
                return value;
            }
            set => _al.SetListenerProperty(ListenerVector3.Position, value);
        }

        public unsafe AudioOrientation Orientation
        {
            get
            {
                AudioOrientation value;
                _al.GetListenerProperty(ListenerFloatArray.Orientation, (float*)&value);
                return value;
            }
            set
            {
                _al.SetListenerProperty(ListenerFloatArray.Orientation, (float*)&value);
            }
        }
    }
}
