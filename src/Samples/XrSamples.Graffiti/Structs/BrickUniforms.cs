using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using XrSamples.Graffiti.Objects;

namespace XrSamples.Graffiti
{
    [StructLayout(LayoutKind.Explicit, Size = 96)]
    public struct BrickUniforms
    {
        [FieldOffset(0)]
        public Vector2 WallSize;

        [FieldOffset(8)]
        public float NoiseScale;

        [FieldOffset(12)]
        public float NoiseStrength;

        [FieldOffset(16)]
        public Vector2 BrickSize;

        [FieldOffset(24)]
        public float OddRowOffset;

        [FieldOffset(28)]
        public float SideDarkening;

        [FieldOffset(32)]
        public Vector2 MortarSize;

        [FieldOffset(40)]
        public float BrickVariation;

        [FieldOffset(44)]
        public float MortarVariation;

        [FieldOffset(48)]
        public Vector2 Offset;

        [FieldOffset(56)]
        public float MinRoughness;

        [FieldOffset(60)]
        public float NormalStrength;

        [FieldOffset(64)]
        public Vector3 BrickColor;

        [FieldOffset(80)]
        public Vector3 MortarColor;

        public static BrickUniforms CreateDefault(BrickGeometry geo)
        {
            return new BrickUniforms
            {
                WallSize = geo.WallSize,

                NoiseScale = 35.0f,
                NoiseStrength = 0.14f,

                BrickSize = geo.BrickSize,
                OddRowOffset = geo.OddRowOffset,
                SideDarkening = 0.30f,

                MortarSize = geo.MortarSize,
                BrickVariation = 0.18f,
                MortarVariation = 0.08f,

                Offset = geo.Offset,
                MinRoughness = 0.78f,
                NormalStrength = 0.007f,

                BrickColor = new Vector3(0.45f, 0.10f, 0.045f),
                MortarColor = new Vector3(0.36f, 0.34f, 0.30f),
            };
        }

    }
}
