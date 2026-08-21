using System.Numerics;
using System.Runtime.InteropServices;
using XrMath.Entities;

[StructLayout(LayoutKind.Explicit, Size = 128)]
public struct VolumeUniforms
{
    [FieldOffset(0)]
    public float Ior;

    [FieldOffset(4)]
    public float Thickness;

    [FieldOffset(8)]
    public float AttenuationDistance;

    [FieldOffset(12)]
    public float TransmissionFactor;

    [FieldOffset(16)]
    public Vector3 AttenuationColor;

    [FieldOffset(32)]
    public Vector4x3 BackgroundUvTransform0;

    [FieldOffset(80)]
    public Vector4x3 BackgroundUvTransform1;
}