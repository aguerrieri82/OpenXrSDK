using System.Runtime.InteropServices;

namespace XrSamples.Graffiti
{
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct PaintStateBuffer
    {
        [FieldOffset(0)]
        public uint HasSprayFragments;

        [FieldOffset(4)]
        public uint SprayMinX;

        [FieldOffset(8)]
        public uint SprayMinY;

        [FieldOffset(12)]
        public uint SprayMaxX;

        [FieldOffset(16)]
        public uint SprayMaxY;

        public void Reset(uint width, uint height)
        {
            HasSprayFragments = 0;

            SprayMinX = width;
            SprayMinY = height;

            SprayMaxX = 0;
            SprayMaxY = 0;
        }
    }
}
