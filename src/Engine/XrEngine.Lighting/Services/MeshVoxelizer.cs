
using System.Numerics;
using XrMath;

namespace XrEngine.Lighting
{

    [Obsolete]
    public sealed class MeshVoxelizer : IDisposable
    {
        private EngineNativeLib.MeshVoxelizer _voxelizer;

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

            Log.Info(this, "Apply transform");

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

            Log.Info(this, "Voxelize");

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

            _voxelizer = default;

            GC.SuppressFinalize(this);
        }

        public VoxelGridDesc GridDesc;

        public VoxelizeMeshParams GridParams;

    }
}
