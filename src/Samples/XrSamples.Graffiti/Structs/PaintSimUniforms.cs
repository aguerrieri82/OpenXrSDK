using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using XrMath;

namespace XrSamples.Graffiti.Shaders;


[StructLayout(LayoutKind.Explicit, Size = 80)]
public struct PaintSimUniforms
{
    // ivec2 CanvasSize;
    [FieldOffset(0)]
    public Vector2I CanvasSize;

    // float DeltaTime;
    [FieldOffset(8)]
    public float DeltaTime;

    // float DryRate;
    [FieldOffset(12)]
    public float DryRate;

    // vec4 PaintColor;
    [FieldOffset(16)]
    public Vector4 PaintColor;

    // float DensityToCoverage;
    [FieldOffset(32)]
    public float DensityToCoverage;

    // float DensityToHeight;
    [FieldOffset(36)]
    public float DensityToHeight;

    // float NormalScale;
    [FieldOffset(40)]
    public float NormalScale;

    // float DryRoughness;
    [FieldOffset(44)]
    public float DryRoughness;

    // float WetRoughness;
    [FieldOffset(48)]
    public float WetRoughness;

    // float Metallic;
    [FieldOffset(52)]
    public float Metallic;


    [FieldOffset(56)]
    public Vector2 GravityCanvas;

    [FieldOffset(64)]
    public float GravityStrength;

    [FieldOffset(68)]
    public float WetDripThreshold;

    [FieldOffset(72)]
    public float WetDripRate;

}