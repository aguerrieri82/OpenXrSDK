using System.Numerics;
using System.Runtime.InteropServices;

namespace XrEngine
{
    [StructLayout(LayoutKind.Explicit, Size = 208)]
    public struct ModelUniforms
    {
        [FieldOffset(0)]
        public Matrix4x4 WorldMatrix;

        [FieldOffset(64)]
        public Matrix4x4 NormalMatrix;

        [FieldOffset(128)]
        public Matrix4x4 PrevWorldMatrix;

        [FieldOffset(192)]
        public int DrawId;
    }
}
