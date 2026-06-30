using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;


namespace XrEngine
{

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoxelGridDesc
    {
        public Vector3 Origin;
        public float VoxelSize;

        public int SizeX;
        public int SizeY;
        public int SizeZ;
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
        public int TriangleId;
        public Vector2 UV;
        public Vector2 HitPosition;
        public VoxelTriangleSide Side;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct VoxelData
    {
        public VoxelStatus Status;
        public float Occupancy;

        public fixed byte Faces[6 * VoxelFaceDataSize];

        public const int VoxelFaceDataSize =
            sizeof(int) +      // TriangleId
            sizeof(float) * 2 +// UV
            sizeof(float) * 2 +// HitPosition
            sizeof(int);       // Side

        public VoxelFaceData GetFace(int index)
        {
            if ((uint)index >= 6u)
                throw new ArgumentOutOfRangeException(nameof(index));

            fixed (byte* pFaces = Faces)
                return ((VoxelFaceData*)pFaces)[index];
        }

        public VoxelFaceData GetFace(VoxelFace face)
        {
            return GetFace((int)face);
        }
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


    public static class EngineNativeLib
    {
        public struct VoxelGrid
        {
            public nint Handle;
        }
        public struct MeshVoxilizer
        {
            public nint Handle;
        }

        const string LibName = "xrengine-native";

        [DllImport(LibName)]
        public static extern void ImageFlipY(nint src, nint dst, uint width, uint height, uint rowSize);

        [DllImport(LibName)]
        public static extern void ImageCopyChannel(nint src, nint dst, uint width, uint height, uint srcRowSize, uint dstRowSize, uint srcOfs, uint dstOfs, uint cSize);

        [DllImport(LibName, EntryPoint = "CopyMemory2")]
        public static extern void CopyMemory(nint src, nint dst, uint size);

        [DllImport(LibName)]
        public static extern int CompareMemory(nint src, nint dst, uint size);

        [DllImport(LibName)]
        public static extern ulong Now();


        [DllImport(LibName)]
        public static extern void SleepUntil(ulong time);

        [DllImport(LibName)]
        public static extern void SleepFor(ulong time);


        [DllImport(LibName)]
        public static unsafe extern void ImagePack(uint srcWidth, uint srcHeight, byte* srcData, uint dstWidth, uint dstHeight, byte* dstData, uint pixelSize);


        [DllImport(LibName)]
        public static unsafe extern void RgbToBgr(uint width, uint height, byte* srcData, byte* dstData, uint pixelSizeByte);


        [DllImport(LibName)]
        public static unsafe extern void ImageResizeBilinearU8(
                uint srcW, uint srcH, byte* src,
                uint dstW, uint dstH, byte* dst,
                uint channels);

        [DllImport(LibName)]
        public static extern int RdcTriggerCapture();

        [DllImport(LibName)]
        public static extern int RdcStartFrameCapture();

        [DllImport(LibName)]
        public static extern int RdcEndFrameCapture(bool launchReplay);



        [DllImport(LibName)]
        public static extern MeshVoxilizer MeshVoxelizerCreate();

        [DllImport(LibName)]
        public static extern void MeshVoxelizerDestroy(MeshVoxilizer voxelizer);

        [DllImport(LibName)]
        public static extern VoxelGrid MeshVoxelizerVoxelize(
            MeshVoxilizer voxelizer,
            VertexData[] vertices,
            int vertexCount,
            uint[] indices,
            int indexCount,
            ref Bounds3 bounds,
            ref VoxelGridDesc grid,
            ref VoxelizeMeshParams parameters);

        [DllImport(LibName)]
        public static extern void MeshVoxelGridDestroy(VoxelGrid voxelGrid);

        [DllImport(LibName)]
        public static unsafe extern MeshVoxelGridView MeshVoxelGridGetView(VoxelGrid voxelGrid);
    }
}
