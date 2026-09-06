using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XrEngine
{

    [StructLayout(LayoutKind.Sequential)]
    public struct MorphTargetUniform
    {
        public float Weight;
        public uint PositionOfs;
        public uint NormalOfs;
        public uint TangentOfs;
    }

    [InlineArray(MorphUniforms.MaxTargets)]
    public struct MorphTargetUniformArray
    {
        private MorphTargetUniform _element0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MorphUniforms
    {
        public const int MaxTargets = 60;

        public MorphTargetUniformArray Targets;
    }
}