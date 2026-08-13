using MeshOptimizerLib = global::MeshOptimizer.MeshOptimizerLib;
using SimplifyOptions = global::MeshOptimizer.MeshOptimizerLib.SimplifyOptions;

namespace XrEngine
{
    public static class MeshOptimizer
    {
        #region Public Structs

        public readonly struct SimplifyResult
        {
            public SimplifyResult(int indexCount, float error)
            {
                IndexCount = indexCount;
                Error = error;
            }

            public int IndexCount { get; }

            public float Error { get; }
        }

        #endregion

        public static unsafe uint[] GenerateVertexRemap<T>(BaseGeometry3D<T> geometry)
            where T : unmanaged, IVertexProvider
        {
            return GenerateVertexRemap(geometry, out _);
        }

        public static unsafe uint[] GenerateVertexRemap<T>(BaseGeometry3D<T> geometry, out int vertexCount)
           where T : unmanaged, IVertexProvider
        {
            var vertices = geometry.Vertices;
            var indices = geometry.Indices;

            var sourceIndices = indices.Length > 0 ? indices : null;
            var indexCount = indices.Length > 0 ? indices.Length : vertices.Length;

            var remap = new uint[vertices.Length];

            fixed (VertexData* pVertices = &vertices[0].Vertex)
            {
                vertexCount = (int)MeshOptimizerLib.meshopt_generateVertexRemap(
                    remap,
                    sourceIndices,
                    indexCount,
                    pVertices,
                    vertices.Length,
                    sizeof(VertexData));
            }

            return remap;
        }

        public static unsafe void RemapVertexBuffer<T>(BaseGeometry3D<T> geometry, uint[] remap, int vertexCount)
            where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var oldVertices = geometry.Vertices;
            var oldIndices = geometry.Indices;

            var newVertices = new T[vertexCount];
            var newIndices = new uint[oldIndices.Length];

            MeshOptimizerLib.meshopt_remapIndexBuffer(
                newIndices,
                oldIndices,
                oldIndices.Length,
                remap);

            fixed (VertexData* pSrc = &oldVertices[0].Vertex)
            fixed (VertexData* pDst = &newVertices[0].Vertex)
            {
                MeshOptimizerLib.meshopt_remapVertexBuffer(
                    pDst,
                    pSrc,
                    oldVertices.Length,
                    sizeof(VertexData),
                    remap);
            }

            geometry.Vertices = newVertices;
            geometry.Indices = newIndices;
        }

        public static void CompactVertices<T>(BaseGeometry3D<T> geometry)
            where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var remap = GenerateVertexRemap(geometry, out var vertexCount);

            RemapVertexBuffer(geometry, remap, vertexCount);
        }

        public static void Optimize<T>(BaseGeometry3D<T> geometry, float overdrawThreshold = 1.05f)
           where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            //CompactVertices(geometry);
            OptimizeVertexCache(geometry);
            OptimizeOverdraw(geometry, overdrawThreshold);
            OptimizeVertexFetch(geometry);
        }

        public static void OptimizeVertexCache<T>(BaseGeometry3D<T> geometry)
           where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var indices = geometry.Indices;
            var result = new uint[indices.Length];

            MeshOptimizerLib.meshopt_optimizeVertexCache(
                result,
                indices,
                indices.Length,
                geometry.Vertices.Length);

            geometry.Indices = result;
        }

        public static void OptimizeOverdraw<T>(BaseGeometry3D<T> geometry, float threshold = 1.05f)
                where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var vertices = geometry.Vertices;
            var indices = geometry.Indices;
            var result = new uint[indices.Length];

            MeshOptimizerLib.meshopt_optimizeOverdraw(
                result,
                indices,
                indices.Length,
                ref vertices[0].Vertex.Pos,
                vertices.Length,
                SizeOfVertex,
                threshold);

            geometry.Indices = result;
        }

        public static unsafe void OptimizeVertexFetch<T>(BaseGeometry3D<T> geometry)
            where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var oldVertices = geometry.Vertices;
            var indices = geometry.Indices;
            var newVertices = new T[oldVertices.Length];

            fixed (VertexData* pSrc = &oldVertices[0].Vertex)
            fixed (VertexData* pDst = &newVertices[0].Vertex)
            {
                var count = MeshOptimizerLib.meshopt_optimizeVertexFetch(
                    pDst,
                    indices,
                    indices.Length,
                    pSrc,
                    oldVertices.Length,
                    sizeof(VertexData));

                Array.Resize(ref newVertices, (int)count);
            }

            geometry.Vertices = newVertices;
            geometry.Indices = indices;
        }

        public static SimplifyResult Simplify<T>(
            BaseGeometry3D<T> geometry,
            float targetIndicesFactor = 0.5f,
            float targetError = 0.01f,
            SimplifyOptions options = SimplifyOptions.None,
            bool compact = true)
            where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var vertices = geometry.Vertices;
            var indices = geometry.Indices;

            var targetIndexCount = GetTargetIndexCount(indices.Length, targetIndicesFactor);
            var result = new uint[indices.Length];

            var count = MeshOptimizerLib.meshopt_simplify(
                result,
                indices,
                indices.Length,
                ref vertices[0].Vertex.Pos,
                vertices.Length,
                SizeOfVertex,
                targetIndexCount,
                targetError,
                (uint)options,
                out var error);

            Array.Resize(ref result, (int)count);

            geometry.Indices = result;

            if (compact)
                OptimizeVertexFetch(geometry);

            return new SimplifyResult((int)count, error);
        }

        public static SimplifyResult SimplifyWithAttributes<T>(BaseGeometry3D<T> geometry,
            float targetIndicesFactor = 0.5f,
            float targetError = 0.01f,
            float normalWeight = 0.1f,
            float uvWeight = 0.1f,
            SimplifyOptions options = SimplifyOptions.None,
            byte[]? vertexLock = null,
            bool compact = true)
             where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var vertices = geometry.Vertices;
            var indices = geometry.Indices;

            var targetIndexCount = GetTargetIndexCount(indices.Length, targetIndicesFactor);
            var result = new uint[indices.Length];

            var weights = new[]
            {
                normalWeight,
                normalWeight,
                normalWeight,
                uvWeight,
                uvWeight
            };

            var count = MeshOptimizerLib.meshopt_simplifyWithAttributes(
                result,
                indices,
                indices.Length,
                ref vertices[0].Vertex.Pos,
                vertices.Length,
                SizeOfVertex,
                ref vertices[0].Vertex.Normal.X,
                SizeOfVertex,
                weights,
                weights.Length,
                vertexLock,
                targetIndexCount,
                targetError,
                (uint)options,
                out var error);

            Array.Resize(ref result, (int)count);

            geometry.Indices = result;

            if (compact)
                OptimizeVertexFetch(geometry);

            return new SimplifyResult((int)count, error);
        }

        public static SimplifyResult SimplifySloppy<T>(BaseGeometry3D<T> geometry,
            float targetIndicesFactor = 0.5f,
            float targetError = 0.01f,
            byte[]? vertexLock = null,
            bool compact = true)
            where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var vertices = geometry.Vertices;
            var indices = geometry.Indices;

            var targetIndexCount = GetTargetIndexCount(indices.Length, targetIndicesFactor);
            var result = new uint[indices.Length];

            var count = MeshOptimizerLib.meshopt_simplifySloppy(
                result,
                indices,
                indices.Length,
                ref vertices[0].Vertex.Pos,
                vertices.Length,
                SizeOfVertex,
                vertexLock,
                targetIndexCount,
                targetError,
                out var error);

            Array.Resize(ref result, (int)count);

            geometry.Indices = result;

            if (compact)
                OptimizeVertexFetch(geometry);

            return new SimplifyResult((int)count, error);
        }

        public static float ComputeSimplifyScale<T>(BaseGeometry3D<T> geometry)
            where T : unmanaged, IVertexProvider
        {
            var vertices = geometry.Vertices;

            return MeshOptimizerLib.meshopt_simplifyScale(
                ref vertices[0].Vertex.Pos,
                vertices.Length,
                SizeOfVertex);
        }

        public static MeshOptimizerLib.VertexCacheStatistics AnalyzeVertexCache<T>(BaseGeometry3D<T> geometry,
            uint cacheSize = 16,
            uint warpSize = 32,
            uint primitiveGroupSize = 0)
            where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            return MeshOptimizerLib.meshopt_analyzeVertexCache(
                geometry.Indices,
                geometry.Indices.Length,
                geometry.Vertices.Length,
                cacheSize,
                warpSize,
                primitiveGroupSize);
        }

        public static MeshOptimizerLib.VertexFetchStatistics AnalyzeVertexFetch<T>(BaseGeometry3D<T> geometry)
                where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            return MeshOptimizerLib.meshopt_analyzeVertexFetch(
                geometry.Indices,
                geometry.Indices.Length,
                geometry.Vertices.Length,
                SizeOfVertex);
        }

        public static MeshOptimizerLib.OverdrawStatistics AnalyzeOverdraw<T>(BaseGeometry3D<T> geometry)
           where T : unmanaged, IVertexProvider
        {
            geometry.EnsureIndices();

            var vertices = geometry.Vertices;

            return MeshOptimizerLib.meshopt_analyzeOverdraw(
                geometry.Indices,
                geometry.Indices.Length,
                ref vertices[0].Vertex.Pos,
                vertices.Length,
                SizeOfVertex);
        }

        private static long GetTargetIndexCount(int indexCount, float factor)
        {
            return (long)(indexCount * factor) / 3 * 3;
        }

        private static int SizeOfVertex => System.Runtime.CompilerServices.Unsafe.SizeOf<VertexData>();
    }
}