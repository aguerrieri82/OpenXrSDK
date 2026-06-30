
using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace XrEngine.Native
{
    public struct VoxelFaceInstance
    {
        public Vector3I Pos;
        public int Face;
    }

    public sealed class MeshVoxelizer : IDisposable
    {
        private EngineNativeLib.MeshVoxilizer _voxelizer;

        public MeshVoxelizer()
        {
            _voxelizer = EngineNativeLib.MeshVoxelizerCreate();

            if (_voxelizer.Handle == 0)
                throw new InvalidOperationException("Failed to create native mesh voxelizer.");
        }

        public unsafe MeshVoxelGrid Voxelize(Geometry3D geometry, Matrix4x4 transform)
        {
            if (geometry.Vertices == null || geometry.Vertices.Length == 0)
                throw new ArgumentException("Geometry has no vertices.", nameof(geometry));

            var srcVertices = geometry.Vertices;
            var dstVertices = new VertexData[srcVertices.Length];

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);

            fixed (VertexData* pSrc = srcVertices)
            fixed (VertexData* pDst = dstVertices)
            {
                for (var i = 0; i < srcVertices.Length; i++)
                {
                    var src = pSrc + i;
                    var dst = pDst + i;

                    var pos = Vector3.Transform(src->Pos, transform);

                    *dst = *src;
                    dst->Pos = pos;
                    dst->Normal = Vector3.TransformNormal(src->Normal, transform);

                    min = Vector3.Min(min, pos);
                    max = Vector3.Max(max, pos);
                }
            }

            var bounds = new Bounds3
            {
                Min = min,
                Max = max
            };

            return Voxelize(dstVertices, geometry.Indices, bounds);
        }

        public MeshVoxelGrid Voxelize(Geometry3D geometry)
        {
            return Voxelize(geometry.Vertices, geometry.Indices, geometry.Bounds);
        }

        public MeshVoxelGrid Voxelize(
            VertexData[] vertices,
            uint[] indices,
            Bounds3 bounds)
        {
            if (_voxelizer.Handle == 0)
                throw new ObjectDisposedException(nameof(MeshVoxelizer));

            var result = EngineNativeLib.MeshVoxelizerVoxelize(
                _voxelizer,
                vertices,
                vertices.Length,
                indices,
                indices.Length,
                ref bounds,
                ref GridDesc,
                ref GridParams);

            if (result.Handle == 0)
                throw new InvalidOperationException("Native mesh voxelization failed.");

            return new MeshVoxelGrid(result);
        }

        public void Dispose()
        {
            if (_voxelizer.Handle == 0)
                return;

            EngineNativeLib.MeshVoxelizerDestroy(_voxelizer);
            _voxelizer.Handle = 0;

            GC.SuppressFinalize(this);
        }

        public VoxelGridDesc GridDesc;

        public VoxelizeMeshParams GridParams;

    }

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
            _voxelGrid.Handle = 0;
        }


        public VoxelFaceInstance[] ExtractFrontFaces()
        {
            var result = new VoxelFaceInstance[_voxelCount * 6];

            fixed (VoxelFaceInstance* pResult = result)
            {
                var count = ExtractFrontFaces(pResult);

                if (count != result.Length)
                    Array.Resize(ref result, count);

                return result;
            }
        }

        public int ExtractFrontFaces(VoxelFaceInstance* dst)
        {
            var count = 0;

            var origin = Info.Origin;
            var size = Info.Size;

            for (var z = 0; z < size.Z; z++)
                for (var y = 0; y < size.Y; y++)
                    for (var x = 0; x < size.X; x++)
                    {
                        ref var voxel = ref this[x, y, z];

                        fixed (byte* pFaces = voxel.Faces)
                        {
                            var faces = (VoxelFaceData*)pFaces;

                            for (var face = 0; face < 6; face++)
                            {
                                if (faces[face].Side != VoxelTriangleSide.Front)
                                    continue;

                                dst[count++] = new VoxelFaceInstance
                                {
                                    Pos = new Vector3I(
                                        origin.X + x,
                                        origin.Y + y,
                                        origin.Z + z),

                                    Face = face
                                };
                            }
                        }
                    }

            return count;
        }

        public MeshVoxelGridInfo Info { get; }

        public int VoxelCount => _voxelCount;

        public ReadOnlySpan<VoxelData> Voxels
        {
            get
            {
                if (_voxelGrid.Handle == 0)
                    throw new ObjectDisposedException(nameof(MeshVoxelGrid));

                return new ReadOnlySpan<VoxelData>(_voxels, _voxelCount);
            }
        }
    }
}
