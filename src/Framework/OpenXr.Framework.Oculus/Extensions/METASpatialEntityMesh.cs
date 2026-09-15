using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenXr.Framework.Oculus
{
    public  class METASpatialEntityMesh : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_spatial_entity_mesh";

        public const SpaceComponentTypeFB SpaceComponentTypeTriangleMeshMeta = (SpaceComponentTypeFB)1000269000;
        public const StructureType TypeSpaceTriangleMeshGetInfoMeta = (StructureType)1000269001;
        public const StructureType TypeSpaceTriangleMeshMeta = (StructureType)1000269002;

        public METASpatialEntityMesh(XR xr, Instance instance) : base(xr, instance)
        {
        }

        [AllowNull]
        public GetSpaceTriangleMeshMETADelegate GetSpaceTriangleMeshMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result GetSpaceTriangleMeshMETADelegate(Space space, ref SpaceTriangleMeshGetInfoMETA getInfo, ref SpaceTriangleMeshMETA triangleMeshOutput);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SpaceTriangleMeshGetInfoMETA
    {
        public readonly StructureType Type;
        public void* Next;

        public SpaceTriangleMeshGetInfoMETA()
        {
            Type = METASpatialEntityMesh.TypeSpaceTriangleMeshGetInfoMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SpaceTriangleMeshMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public uint VertexCapacityInput;
        public uint VertexCountOutput;
        public Vector3f* Vertices;
        public uint IndexCapacityInput;
        public uint IndexCountOutput;
        public uint* Indices;

        public SpaceTriangleMeshMETA()
        {
            Type = METASpatialEntityMesh.TypeSpaceTriangleMeshMeta;
        }
    }
}