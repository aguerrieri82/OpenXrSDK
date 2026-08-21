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

        public static unsafe uint[] GenerateVertexRemap(Geometry3D geometry)
        {
            return GenerateVertexRemap(geometry, out _);
        }

        public static unsafe uint[] GenerateVertexRemap(Geometry3D geometry, out int vertexCount)
        {
            var vertices = geometry.Vertices;
            var indices = geometry.Indices;

            var sourceIndices = indices.Length > 0 ? indices : null;
            var indexCount = indices.Length > 0 ? indices.Length : vertices.Length;

            var remap = new uint[vertices.Length];

            fixed (VertexData* pVertices = vertices)
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

        public static unsafe void RemapVertexBuffer(Geometry3D geometry, uint[] remap, int vertexCount)
        {
            geometry.EnsureIndices();

            var oldVertices = geometry.Vertices;
            var oldIndices = geometry.Indices;

            var newVertices = new VertexData[vertexCount];
            var newIndices = new uint[oldIndices.Length];

            MeshOptimizerLib.meshopt_remapIndexBuffer(
                newIndices,
                oldIndices,
                oldIndices.Length,
                remap);

            fixed (VertexData* pSrc = oldVertices)
            fixed (VertexData* pDst = newVertices)
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

        public static void CompactVertices(Geometry3D geometry)
        {
            geometry.EnsureIndices();

            var remap = GenerateVertexRemap(geometry, out var vertexCount);

            RemapVertexBuffer(geometry, remap, vertexCount);
        }

        public static void Optimize(Geometry3D geometry, float overdrawThreshold = 1.05f)
        {
            geometry.EnsureIndices();

            //CompactVertices(geometry);
            OptimizeVertexCache(geometry);
            OptimizeOverdraw(geometry, overdrawThreshold);
            OptimizeVertexFetch(geometry);
        }

        public static void OptimizeVertexCache(Geometry3D geometry)
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

        public static void OptimizeOverdraw(Geometry3D geometry, float threshold = 1.05f)
        {
            geometry.EnsureIndices();

            var vertices = geometry.Vertices;
            var indices = geometry.Indices;
            var result = new uint[indices.Length];

            MeshOptimizerLib.meshopt_optimizeOverdraw(
                result,
                indices,
                indices.Length,
                ref vertices[0].Pos,
                vertices.Length,
                SizeOfVertex,
                threshold);

            geometry.Indices = result;
        }

        public static unsafe void OptimizeVertexFetch(Geometry3D geometry)
        {
            geometry.EnsureIndices();

            var oldVertices = geometry.Vertices;
            var indices = geometry.Indices;
            var newVertices = new VertexData[oldVertices.Length];

            fixed (VertexData* pSrc = oldVertices)
            fixed (VertexData* pDst = newVertices)
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

        public static SimplifyResult Simplify(
            Geometry3D geometry,
            float targetIndicesFactor = 0.5f,
            float targetError = 0.01f,
            SimplifyOptions options = SimplifyOptions.None,
            bool compact = true)
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
                ref vertices[0].Pos,
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

        public static SimplifyResult SimplifyWithAttributes(
            Geometry3D geometry,
            float targetIndicesFactor = 0.5f,
            float targetError = 0.01f,
            float normalWeight = 0.1f,
            float uvWeight = 0.1f,
            SimplifyOptions options = SimplifyOptions.None,
            byte[]? vertexLock = null,
            bool compact = true)
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
                ref vertices[0].Pos,
                vertices.Length,
                SizeOfVertex,
                ref vertices[0].Normal.X,
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

        public static SimplifyResult SimplifySloppy(
            Geometry3D geometry,
            float targetIndicesFactor = 0.5f,
            float targetError = 0.01f,
            byte[]? vertexLock = null,
            bool compact = true)
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
                ref vertices[0].Pos,
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

        public static float ComputeSimplifyScale(Geometry3D geometry)
        {
            var vertices = geometry.Vertices;

            return MeshOptimizerLib.meshopt_simplifyScale(
                ref vertices[0].Pos,
                vertices.Length,
                SizeOfVertex);
        }

        public static MeshOptimizerLib.VertexCacheStatistics AnalyzeVertexCache(
            Geometry3D geometry,
            uint cacheSize = 16,
            uint warpSize = 32,
            uint primitiveGroupSize = 0)
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

        public static MeshOptimizerLib.VertexFetchStatistics AnalyzeVertexFetch(Geometry3D geometry)
        {
            geometry.EnsureIndices();

            return MeshOptimizerLib.meshopt_analyzeVertexFetch(
                geometry.Indices,
                geometry.Indices.Length,
                geometry.Vertices.Length,
                SizeOfVertex);
        }

        public static MeshOptimizerLib.OverdrawStatistics AnalyzeOverdraw(Geometry3D geometry)
        {
            geometry.EnsureIndices();

            var vertices = geometry.Vertices;

            return MeshOptimizerLib.meshopt_analyzeOverdraw(
                geometry.Indices,
                geometry.Indices.Length,
                ref vertices[0].Pos,
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