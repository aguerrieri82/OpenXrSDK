using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public interface IXrInput : IXrAction
    {
        void Update(Space refSpace, long predictTime);

        public DateTime LastChangeTime { get; }

        public bool IsActive { get; }

        public bool IsChanged { get; }

        public object Value { get; }

        public string Path { get; }
    }
}
