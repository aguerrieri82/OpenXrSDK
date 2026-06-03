using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrSamples.Graffiti.Shaders;


[StructLayout(LayoutKind.Explicit, Size = 80)]
public struct PaintSimUniforms
{
    [FieldOffset(0)]
    public Vector2I CanvasSize;

    [FieldOffset(8)]
    public float DeltaTime;

    [FieldOffset(12)]
    public float DryRate;

    [FieldOffset(16)]
    public Vector4 PaintColor;

    [FieldOffset(32)]
    public float DensityToCoverage;

    [FieldOffset(36)]
    [Obsolete]
    public float DensityToHeight;

    [FieldOffset(40)]
    public float NormalScale;

    [FieldOffset(44)]
    public float DryRoughness;

    [FieldOffset(48)]
    public float WetRoughness;

    [FieldOffset(52)]
    public float Metallic;

    [FieldOffset(56)]
    public Vector2 GravityCanvas;

    [FieldOffset(64)]
    public float GravityStrength;

    [FieldOffset(68)]
    [Obsolete]
    public float WetDripThreshold;

    [FieldOffset(72)]
    public float WetDripRate;

}