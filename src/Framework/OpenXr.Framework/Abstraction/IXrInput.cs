using Silk.NET.OpenXR;
using System.Numerics;

namespace OpenXr.Framework
{
    public struct XrInputState
    {
        public bool IsActive;

        public bool IsChanged;

        public object Value;

        public Vector3? LinearVelocity;

        public Vector3? AngularVelocity;
    }


    public interface IXrInput : IXrAction
    {
        void Update(Space refSpace, long predictTime);

        XrInputState GetState();

        void SetState(XrInputState value);

        public DateTime LastChangeTime { get; }

        public bool IsActive { get; }

        public bool IsChanged { get; }

        public object Value { get; }

        public string Path { get; }
    }
}
