using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XrEngine
{

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct MorphTargetUniform
    {
        public float Weight;
        public uint PositionOfs;
        public uint NormalOfs;
        public uint TangentOfs;
        public uint Uv0Ofs;
    }

    [InlineArray(MorphUniforms.MaxTargets)]
    public struct MorphTargetUniformArray
    {
        private MorphTargetUniform _element0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MorphUniforms
    {
        public const int MaxTargets = 180;

        public MorphTargetUniformArray Targets;
    }
}