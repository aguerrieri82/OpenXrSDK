using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.Lighting
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VoxelLightBakeParamsV2
    {
        public VoxelLightBakeParams Base;

        public float AngularTolerance;
        public float RelativeEnergyTolerance;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VoxelLightContributionSampleV2
    {
        public Vector3 Direction;
        public Vector3 Energy;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VoxelLightContributionCellV2
    {
        public int Index;
        public uint Offset;
        public uint Count;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VoxelLightLookup
    {
        public uint Offset;
        public uint Count;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VoxelLightGpuContribution
    {
        public Vector4 Direction;
        public Vector4 Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VoxelLightContributionViewV2
    {
        public VoxelLightContributionCellV2* Cells;
        public int CellCount;
        public int CellCapacity;

        public VoxelLightContributionSampleV2* Samples;
        public int SampleCount;
        public int SampleCapacity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VoxelLightFieldViewV2
    {
        public Vector3I Size;

        public VoxelLightLookup* Lookup;
        public int LookupCount;
        public int LookupCapacity;

        public VoxelLightGpuContribution* Contributions;
        public int ContributionCount;
        public int ContributionCapacity;
    }

    public static class EngineNativeLibV2
    {
        private const string LibName = "xrengine-native";

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct VoxelLightBakerV2
        {
            public readonly nint Handle;

            public VoxelLightBakerV2(nint handle)
            {
                Handle = handle;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct VoxelRayMarcherV2
        {
            public readonly nint Handle;

            public VoxelRayMarcherV2(nint handle)
            {
                Handle = handle;
            }
        }

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern VoxelLightBakerV2 VoxelLightBakerV2Create();

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerV2Destroy(
            VoxelLightBakerV2 baker);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerV2SetParams(
            VoxelLightBakerV2 baker,
            ref VoxelLightBakeParamsV2 parameters);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerV2SetGrid(
            VoxelLightBakerV2 baker,
            ref VoxelGridDesc grid);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerV2ClearScene(
            VoxelLightBakerV2 baker);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerV2AddMesh(
            VoxelLightBakerV2 baker,
            ref Vector3I origin,
            ref Vector3I size,
            VoxelData[] voxels,
            VoxelMeshResolvedFace[] faces,
            int faceCount);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerV2AddGpuMeshFaces(
            VoxelLightBakerV2 baker,
            [In] GpuVoxelFaceData[] faces,
            int faceCount);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern unsafe VoxelData* VoxelLightBakerV2GetScene(
            VoxelLightBakerV2 baker,
            out int count);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelLightBakerV2BakePointLight(
            VoxelLightBakerV2 baker,
            ref VoxPointLight light,
            ref VoxelLightContributionViewV2 contribution);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelLightBakerV2BakeAreaLight(
            VoxelLightBakerV2 baker,
            ref VoxAreaLight light,
            ref VoxelLightContributionViewV2 contribution);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelLightBakerV2BakeDirectionalLight(
            VoxelLightBakerV2 baker,
            ref VoxDirectionalLight light,
            ref VoxelLightContributionViewV2 contribution);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelLightBakerV2BakeSpotLight(
            VoxelLightBakerV2 baker,
            ref VoxSpotLight light,
            ref VoxelLightContributionViewV2 contribution);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerV2ClearLightField(
            VoxelLightBakerV2 baker);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelLightBakerV2AccumulateLight(
            VoxelLightBakerV2 baker,
            ref VoxelLightContributionViewV2 contribution);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelLightBakerV2GetLightField(
            VoxelLightBakerV2 baker,
            ref VoxelLightFieldViewV2 field);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelLightBakerV2BuildLightField(
            VoxelLightBakerV2 baker,
            float angularTolerance,
            float relativeEnergyTolerance,
            ref VoxelLightFieldViewV2 field);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern VoxelRayMarcherV2 VoxelRayMarcherV2Create(
            VoxelLightBakerV2 baker);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelRayMarcherV2Destroy(
            VoxelRayMarcherV2 marcher);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool VoxelRayMarcherV2CreateRay(
            VoxelRayMarcherV2 marcher,
            ref VoxelLightRay ray);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool VoxelRayMarcherV2Step(
            VoxelRayMarcherV2 marcher);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void VoxelRayMarcherV2GetState(
            VoxelRayMarcherV2 marcher,
            ref VoxelRayDebugState state);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern int VoxelRayMarcherV2GetContribution(
            VoxelRayMarcherV2 marcher,
            ref VoxelLightContributionViewV2 contribution);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void FreeLightFieldViewV2(
            ref VoxelLightFieldViewV2 view);

        [DllImport(LibName, CallingConvention = CallingConvention.Winapi)]
        public static extern void FreeContributionViewV2(
            ref VoxelLightContributionViewV2 view);
    }
}
