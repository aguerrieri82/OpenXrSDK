    using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace XrSamples.Graffiti.Shaders
{
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    public struct PaintProjUniforms
    {
        [FieldOffset(0)]
        public Matrix4x4 HostLocalToWorld;

        [FieldOffset(64)]
        public Matrix4x4 CanvasWorldToLocal;

        [FieldOffset(128)]
        public Matrix4x4 CanvasLocalToWorld;

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
        public float DistanceFalloff;

        [FieldOffset(248)]
        public float RadialFalloff;
    }
}