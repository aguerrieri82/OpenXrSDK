using System.Numerics;
using System.Runtime.InteropServices;

namespace XrEngine
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct DepthObjectData
    {
        [FieldOffset(0)]
        public Vector3 BoundsMax;

        [FieldOffset(16)]
        public Vector3 BoundsMin;

        [FieldOffset(32)]
        public Vector2 Extent;

        [FieldOffset(40)]
        public bool IsVisible;

        [FieldOffset(44)]
        public bool IsCulled;

        [FieldOffset(48)]
        public Vector3 DepthSample;

        [FieldOffset(60)]
        public uint DrawId;
    }


    public interface IDepthCullProvider
    {
        IBuffer<DepthObjectData> DepthCullBuffer { get; }

        bool IsActive { get; }
    }
}
