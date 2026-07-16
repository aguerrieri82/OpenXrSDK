using XrMath;

namespace XrEngine.Lighting
{
    [Obsolete]
    public sealed unsafe class MeshVoxelGrid : IDisposable
    {
        private EngineNativeLib.VoxelGrid _voxelGrid;
        private readonly VoxelData* _voxels;
        private readonly int _voxelCount;

        internal MeshVoxelGrid(EngineNativeLib.VoxelGrid voxelGrid)
        {
            if (voxelGrid.Handle == 0)
                throw new ArgumentException("Invalid native voxel grid.", nameof(voxelGrid));

            _voxelGrid = voxelGrid;

            var view = EngineNativeLib.MeshVoxelGridGetView(voxelGrid);

            Info = view.Info;
            _voxels = view.Voxels;
            _voxelCount = view.VoxelCount;
        }

        public ref VoxelData this[int x, int y, int z]
        {
            get
            {
                var size = Info.Size;
                return ref _voxels[x + y * size.X + z * size.X * size.Y];
            }
        }

        public ref VoxelData this[Vector3I pos]
        {
            get => ref this[pos.X, pos.Y, pos.Z];
        }

        public VoxelData[] ToArray()
        {
            var result = new VoxelData[_voxelCount];
            Voxels.CopyTo(result);
            return result;
        }

        public void Dispose()
        {
            if (_voxelGrid.Handle == 0)
                return;

            EngineNativeLib.MeshVoxelGridDestroy(_voxelGrid);

            _voxelGrid = default;
        }


        public GpuVoxelFaceInstance[] ExtractFaces(VoxelTriangleSide side)
        {
            var result = new GpuVoxelFaceInstance[_voxelCount * 6];

            fixed (GpuVoxelFaceInstance* pResult = result)
            {
                var count = ExtractFaces(pResult, side);

                if (count != result.Length)
                    Array.Resize(ref result, count);

                return result;
            }
        }

        public int ExtractFaces(GpuVoxelFaceInstance* dst, VoxelTriangleSide side)
        {
            var count = 0;

            var origin = Info.Origin;
            var size = Info.Size;

            Log.Info(this, "Extract Front Faces");

            for (var z = 0; z < size.Z; z++)
            {
                for (var y = 0; y < size.Y; y++)
                {
                    for (var x = 0; x < size.X; x++)
                    {
                        ref var voxel = ref this[x, y, z];

                        for (var face = 0; face < VoxelLightConst.FaceCount; face++)
                        {
                            ref var faceData = ref voxel.Faces[face];

                            if ((faceData.Side & side) == 0)
                                continue;

                            dst[count++] = new GpuVoxelFaceInstance
                            {
                                Pos = new Vector3I(
                                    origin.X + x,
                                    origin.Y + y,
                                    origin.Z + z),

                                Face = face,
                                //UV = faceData.UV,
                                //TriangleId = faceData.TriangleId
                            };
                        }
                    }
                }
            }

            return count;
        }

        public MeshVoxelGridInfo Info { get; }

        public int VoxelCount => _voxelCount;

        public Span<VoxelData> Voxels
        {
            get
            {
                if (_voxelGrid.Handle == 0)
                    throw new ObjectDisposedException(nameof(MeshVoxelGrid));

                return new Span<VoxelData>(_voxels, _voxelCount);
            }
        }
    }
}
