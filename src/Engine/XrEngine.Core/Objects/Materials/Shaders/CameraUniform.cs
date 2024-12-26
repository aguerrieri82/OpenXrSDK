using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    [StructLayout(LayoutKind.Explicit, Size = 400)]
    public struct CameraUniforms
    {
        [FieldOffset(0)]

        public Matrix4x4 ViewProj;

        [FieldOffset(64)]

        public Vector3 Position;

        [FieldOffset(76)]
        public float Exposure;

        [FieldOffset(80)]
        public Matrix4x4 LightSpaceMatrix;

        [FieldOffset(144)]
        public int ActiveEye;

        [FieldOffset(152)]
        public Size2I ViewSize;

        [FieldOffset(160)]
        public float NearPlane;

        [FieldOffset(164)]
        public float FarPlane;

        [FieldOffset(168)]
        public float DepthNoiseFactor;

        [FieldOffset(172)]
        public float DepthNoiseDistance;

        [FieldOffset(176)]
        public Plane FrustumPlane1;
        [FieldOffset(192)]
        public Plane FrustumPlane2;
        [FieldOffset(208)]
        public Plane FrustumPlane3;
        [FieldOffset(224)]
        public Plane FrustumPlane4;
        [FieldOffset(240)]
        public Plane FrustumPlane5;
        [FieldOffset(256)]
        public Plane FrustumPlane6;

        [FieldOffset(272)]
        public Matrix4x4 View;

        [FieldOffset(336)]
        public Matrix4x4 Proj;
    }
}
