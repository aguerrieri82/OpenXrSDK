using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace XrSamples.Graffiti
{

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct PaintLayerParams
    {
        [FieldOffset(0)]
        public float DryRateToNext;

        [FieldOffset(4)]
        public float Wetness;

        [FieldOffset(8)]
        public float DripRate;

        [FieldOffset(12)]
        public float DripThreshold;

        [FieldOffset(16)]
        public float MixStrength;

        [FieldOffset(20)]
        public float StainStrength;

        // std140 padding: bytes 24..31
    }

    [InlineArray(8)]
    public struct PaintLayerParamsArray
    {
        private PaintLayerParams _element0;
    }

    [StructLayout(LayoutKind.Explicit, Size = 336)]
    public struct PaintSimulationBlock
    {
        [FieldOffset(0)]
        public Vector2 CanvasSize;

        [FieldOffset(8)]
        public float DeltaTime;

        [FieldOffset(12)]
        public int LayerCount;

        [FieldOffset(16)]
        public Vector3 SprayColor;

        [FieldOffset(28)]
        public float SprayDensityScale;

        [FieldOffset(32)]
        public Vector2 GravityCanvas;

        [FieldOffset(40)]
        public float GravityStrength;

        [FieldOffset(44)]
        public float GlobalDryScale;

        [FieldOffset(48)]
        public float GlobalDripScale;

        [FieldOffset(52)]
        public float GlobalMixScale;

        [FieldOffset(56)]
        public float DryRoughness;

        [FieldOffset(60)]
        public float WetRoughness;

        [FieldOffset(64)]
        public float HeightScale;

        [FieldOffset(68)]
        public float DensityToHeight;

        [FieldOffset(80)]
        public PaintLayerParamsArray Layers;
    }
}
