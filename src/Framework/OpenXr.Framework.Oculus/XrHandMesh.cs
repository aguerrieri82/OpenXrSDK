using Silk.NET.Maths;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Oculus
{
    public struct XrHandJoint
    {
        public Posef BindPose;

        public HandJointEXT Parent;

        public float Radii;
    }

    public struct XrHandVertex
    {
        public Vector3f Pos;

        public Vector3f Normal;

        public Vector2f UV;

        public Vector4D<short> BlendIndex;

        public Vector4f BlendWeight;
    }

    public class XrHandMesh
    {
        public XrHandJoint[]? Joints;

        public XrHandVertex[]? Vertices;

        public HandCapsuleFB[]? Capsules;

        public uint[]? Indices;
    }
}
