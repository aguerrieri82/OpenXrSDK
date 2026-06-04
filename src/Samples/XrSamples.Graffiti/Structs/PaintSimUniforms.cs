using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrSamples.Graffiti.Shaders;


[StructLayout(LayoutKind.Explicit, Size = 96)]
public struct PaintSimUniforms
{
    // ivec2 CanvasSize
    [FieldOffset(0)]
    public Vector2I CanvasSize;

    // ivec2 ComputeOffset
    [FieldOffset(8)]
    public Vector2I ComputeOffset;

    // ivec2 ComputeSize
    [FieldOffset(16)]
    public Vector2I ComputeSize;

    // float DeltaTime
    [FieldOffset(24)]
    public float DeltaTime;

    // float DryRate
    [FieldOffset(28)]
    public float DryRate;

    // float PaintOpacityScale
    [FieldOffset(32)]
    public float PaintOpacityScale;

    // float NormalScale
    [FieldOffset(36)]
    public float NormalScale;

    // std140 padding: 40..47

    // vec4 PaintColor
    [FieldOffset(48)]
    public Vector4 PaintColor;

    // float DryRoughness
    [FieldOffset(64)]
    public float DryRoughness;

    // float WetRoughness
    [FieldOffset(68)]
    public float WetRoughness;

    // float Metallic
    [FieldOffset(72)]
    public float Metallic;

    // float WetDripRate
    [FieldOffset(76)]
    public float WetDripRate;

    // vec2 GravityCanvas
    [FieldOffset(80)]
    public Vector2 GravityCanvas;

    // float GravityStrength
    [FieldOffset(88)]
    public float GravityStrength;
}