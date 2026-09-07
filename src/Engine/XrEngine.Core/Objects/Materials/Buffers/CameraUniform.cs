using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    public struct CameraViewUniforms
    {
        [FieldOffset(0)]
        public Matrix4x4 ViewProj;

        [FieldOffset(64)]
        public Vector3 Position;

        [FieldOffset(80)]
        public Matrix4x4 ViewProjInv;
    }

    [InlineArray(2)]
    public struct CameraViewsUniforms
    {
        private CameraViewUniforms _element0;
    }

    [StructLayout(LayoutKind.Explicit, Size = 624)]
    public struct CameraUniforms
    {
        [FieldOffset(0)]
        public CameraViewsUniforms Eyes;

        [FieldOffset(288)]
        public float Exposure;

        [FieldOffset(304)]
        public Matrix4x4 LightSpaceMatrix;

        [FieldOffset(368)]
        public int ActiveEye;

        [FieldOffset(376)]
        public Size2I ViewSize;

        [FieldOffset(384)]
        public float NearPlane;

        [FieldOffset(388)]
        public float FarPlane;

        [FieldOffset(392)]
        public float DepthNoiseFactor;

        [FieldOffset(396)]
        public float DepthNoiseDistance;

        [FieldOffset(400)]
        public Plane FrustumPlane1;
        [FieldOffset(416)]
        public Plane FrustumPlane2;
        [FieldOffset(432)]
        public Plane FrustumPlane3;
        [FieldOffset(448)]
        public Plane FrustumPlane4;
        [FieldOffset(464)]
        public Plane FrustumPlane5;
        [FieldOffset(480)]
        public Plane FrustumPlane6;

        [FieldOffset(496)]
        public Matrix4x4 View;

        [FieldOffset(560)]
        public Matrix4x4 Proj;
    }
}
