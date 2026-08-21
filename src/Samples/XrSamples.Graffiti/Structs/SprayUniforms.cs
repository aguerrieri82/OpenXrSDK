using System.Numerics;
using System.Runtime.InteropServices;

namespace XrSamples.Graffiti.Shaders
{
    [StructLayout(LayoutKind.Explicit, Size = 352)]
    public struct SprayUniforms
    {
        [FieldOffset(0)]
        public Matrix4x4 CanWorld;

        [FieldOffset(64)]
        public Matrix4x4 CanvasWorldInverse;

        [FieldOffset(128)]
        public Matrix4x4 CanvasWorld;

        [FieldOffset(192)]
        public Vector3 SprayCenterLocal;

        [FieldOffset(208)]
        public Vector3 SprayDirectionLocal;

        [FieldOffset(220)]
        public float SprayRadius;

        [FieldOffset(224)]
        public float SpreadAngle;

        [FieldOffset(232)]
        public Vector2 CanvasSize;

        [FieldOffset(240)]
        public float DensityScale;

        [FieldOffset(244)]
        [Obsolete]
        public float DistanceFalloff;

        [FieldOffset(248)]
        public float RadialFalloff;

        [FieldOffset(256)]
        public Vector3 PrevPosition;

        [FieldOffset(272)]
        public Quaternion PrevRotation;

        [FieldOffset(288)]
        public Vector3 CurPosition;

        [FieldOffset(304)]
        public Quaternion CurRotation;

        [FieldOffset(320)]
        public int StepCount;

        [FieldOffset(336)]
        public Vector3 CanScale;
    }
}