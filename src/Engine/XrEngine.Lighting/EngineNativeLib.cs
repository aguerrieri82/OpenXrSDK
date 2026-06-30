using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrMath;


namespace XrEngine.Lighting
{
    public static class VoxelLightConst
    {
        public const int FaceCount = 6;
    }

    [InlineArray(VoxelLightConst.FaceCount)]
    public struct VoxelLightFaceArray
    {
        private VoxelLightFace _element0;
    }

    [InlineArray(VoxelLightConst.FaceCount)]
    public struct VoxelResolvedFaceArray
    {
        private VoxelResolvedFace _element0;
    }

    [InlineArray(VoxelLightConst.FaceCount)]
    public struct NativePtrArray6
    {
        private nint _element0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelGridDesc
    {
        public Vector3 Origin;
        public float VoxelSize;
        public Vector3I Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelizeMeshParams
    {
        public int ScanSubdiv;
    }

    public enum VoxelStatus : int
    {
        Free = 0,
        Occupied = 1
    }

    public enum VoxelTriangleSide : int
    {
        None = 0,
        Front = 1,
        Back = 2
    }

    public enum VoxelFace : int
    {
        NegX = 0,
        PosX = 1,
        NegY = 2,
        PosY = 3,
        NegZ = 4,
        PosZ = 5
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelFaceData
    {
        public Vector2 UV;
        public Vector2 HitPosition;
        public int TriangleId;
        public VoxelTriangleSide Side;
    }

    [InlineArray(VoxelLightConst.FaceCount)]
    public struct VoxelFaceDataArray
    {
        private VoxelFaceData _element0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct VoxelData
    {
        public VoxelStatus Status;
        public float Occupancy;

        public VoxelFaceDataArray Faces;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MeshVoxelGridInfo
    {
        public Vector3I Origin;
        public Vector3I Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct MeshVoxelGridView
    {
        public MeshVoxelGridInfo Info;
        public VoxelData* Voxels;
        public int VoxelCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelResolvedFace
    {
        public Vector4 BaseColor;
        public Vector3 Normal;

        public float Roughness;
        public float Metallic;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxPointLight
    {
        public Vector3 Position;
        public Vector3 Color;

        public float Intensity;
        public float FalloffDistance;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelMeshResolvedFace
    {
        public int VoxelIndex;
        public int Face;

        public VoxelFaceData Data;
        public VoxelResolvedFace Resolved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelLightBakeParams
    {
            public float RaySpacingFactor;
            public float EmptyDissipation;
            public float EnergyThreshold;

            public int MaxBounceCount;
            public int ThreadCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelLightEnergy
    {
        public Vector3 Energy;

        public Vector3 DirectionR;
        public Vector3 DirectionG;
        public Vector3 DirectionB;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelLightFace
    {
        public VoxelLightEnergy Incoming;
        public VoxelLightEnergy Outgoing;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelLightData
    {
        public VoxelLightFaceArray Faces;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelLightCell
    {
        public int Index;
        public VoxelLightData Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct VoxelLightContributionView
    {
        public VoxelLightCell* Cells;
        public int CellCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelLightFieldView
    {
        public int SizeX;
        public int SizeY;
        public int SizeZ;

        public NativePtrArray6 Color;
        public NativePtrArray6 Direction;

        public int CellCount;

        public int CellCapacity;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelLightRay
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 Energy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelRayDebugState
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 Energy;

        public Vector3I Cell;

        public int LastHitVoxel;
        public int LastAffectedVoxel;
        public int LastAffectedFace;

        public int BounceCount;

        public VoxelData LastVoxel;
        public VoxelLightData LastLightData;

        public VoxelResolvedFaceArray LastResolvedFaces;
    }

    public static class EngineNativeLib
    {
        private const string LibName = "xrengine-native";

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct VoxelGrid
        {
            public readonly nint Handle;

            public VoxelGrid(nint handle)
            {
                Handle = handle;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct MeshVoxelizer
        {
            public readonly nint Handle;

            public MeshVoxelizer(nint handle)
            {
                Handle = handle;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct VoxelLightBaker
        {
            public readonly nint Handle;

            public VoxelLightBaker(nint handle)
            {
                Handle = handle;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct VoxelRayMarcher
        {
            public readonly nint Handle;

            public VoxelRayMarcher(nint handle)
            {
                Handle = handle;
            }
        }

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern MeshVoxelizer MeshVoxelizerCreate();

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void MeshVoxelizerDestroy(
            MeshVoxelizer voxelizer);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern VoxelGrid MeshVoxelizerVoxelize(
            MeshVoxelizer voxelizer,
            VertexData[] vertices,
            int vertexCount,
            uint[] indices,
            int indexCount,
            ref Bounds3 bounds,
            ref VoxelGridDesc grid,
            ref VoxelizeMeshParams parameters);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void MeshVoxelGridDestroy(
            VoxelGrid voxelGrid);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern MeshVoxelGridView MeshVoxelGridGetView(
            VoxelGrid voxelGrid);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern VoxelLightBaker VoxelLightBakerCreate();

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerDestroy(
            VoxelLightBaker baker);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerSetParams(
            VoxelLightBaker baker,
            ref VoxelLightBakeParams parameters);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerSetGrid(
            VoxelLightBaker baker,
            ref VoxelGridDesc grid);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerClearScene(
            VoxelLightBaker baker);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerAddMesh(
            VoxelLightBaker baker,
            ref Vector3I origin,
            ref Vector3I size,
            VoxelData[] voxels,
            VoxelMeshResolvedFace[] faces,
            int faceCount);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelLightBakerBakePointLight(
            VoxelLightBaker baker,
            ref VoxPointLight light,
            ref VoxelLightContributionView contribution);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerClearLightField(
            VoxelLightBaker baker);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerAccumulateLight(
            VoxelLightBaker baker,
            ref VoxelLightContributionView contribution);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelLightBakerGetLightField(
            VoxelLightBaker baker,
            ref VoxelLightFieldView field);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern VoxelRayMarcher VoxelRayMarcherCreate(
            VoxelLightBaker baker);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelRayMarcherDestroy(
            VoxelRayMarcher marcher);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool VoxelRayMarcherCreateRay(
            VoxelRayMarcher marcher,
            ref VoxelLightRay ray);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool VoxelRayMarcherStep(
            VoxelRayMarcher marcher);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelRayMarcherGetState(
            VoxelRayMarcher marcher,
            ref VoxelRayDebugState state);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelRayMarcherGetContribution(
            VoxelRayMarcher marcher,
            ref VoxelLightContributionView contribution);


        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void FreeLightFieldView(ref VoxelLightFieldView view);
    }
}