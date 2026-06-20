using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct SplatData
    {
        // vec3 Position; padded to 16 by GLSL struct layout
        [FieldOffset(0)]
        public Vector3 Position;

        // vec4 AxisX
        [FieldOffset(16)]
        public Vector4 AxisX;

        // vec4 AxisY
        [FieldOffset(32)]
        public Vector4 AxisY;

        // vec4 Color
        [FieldOffset(48)]
        public Vector4 Color;
    }
}
