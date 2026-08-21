using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct IridescenceUniforms
    {
        [FieldOffset(0)]
        public float Factor;

        [FieldOffset(4)]
        public float Ior;

        [FieldOffset(8)]
        public float ThicknessMinimum;

        [FieldOffset(12)]
        public float ThicknessMaximum;
    }
}
