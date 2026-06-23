using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshOptimizer
{
    public static unsafe class MeshOptimizerLib
    {
        private const string DllName = "meshoptimizer-native";

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct MeshOptStream
        {
            public void* Data;
            public long Size;
            public long Stride;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexCacheStatistics
        {
            public uint VerticesTransformed;
            public uint WarpsExecuted;
            public float Acmr;
            public float Atvr;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexFetchStatistics
        {
            public uint BytesFetched;
            public float Overfetch;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OverdrawStatistics
        {
            public uint PixelsCovered;
            public uint PixelsShaded;
            public float Overdraw;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CoverageStatistics
        {
            public Vector3 Coverage;
            public float Extent;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Meshlet
        {
            public uint VertexOffset;
            public uint TriangleOffset;
            public uint VertexCount;
            public uint TriangleCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Bounds
        {
            public Vector3 Center;
            public float Radius;

            public Vector3 ConeApex;
            public Vector3 ConeAxis;
            public float ConeCutoff;

            public sbyte ConeAxisS8X;
            public sbyte ConeAxisS8Y;
            public sbyte ConeAxisS8Z;
            public sbyte ConeCutoffS8;
        }

        #endregion

        #region Enums

        [Flags]
        public enum SimplifyOptions : uint
        {
            None = 0,
            LockBorder = 1 << 0,
            Sparse = 1 << 1,
            ErrorAbsolute = 1 << 2,
            Prune = 1 << 3,
            Regularize = 1 << 4,
            Permissive = 1 << 5,
            RegularizeLight = 1 << 6
        }

        [Flags]
        public enum SimplifyVertexFlags : byte
        {
            None = 0,
            Lock = 1 << 0,
            Protect = 1 << 1,
            Priority = 1 << 2
        }

        [Flags]
        public enum TangentOptions : uint
        {
            None = 0,
            Compatible = 1 << 0,
            ZeroFallback = 1 << 1
        }

        #endregion

        #region Remap / Reindex

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_generateVertexRemap(
            uint[] destination,
            uint[]? indices,
            long index_count,
            void* vertices,
            long vertex_count,
            long vertex_size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_generateVertexRemapMulti(
            uint[] destination,
            uint[]? indices,
            long index_count,
            long vertex_count,
            MeshOptStream[] streams,
            long stream_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_remapVertexBuffer(
            void* destination,
            void* vertices,
            long vertex_count,
            long vertex_size,
            uint[] remap);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_remapIndexBuffer(
            uint[] destination,
            uint[]? indices,
            long index_count,
            uint[] remap);

        #endregion

        #region Filter / Shadow / Position-Only

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_filterIndexBuffer(
            uint[] destination,
            uint[] indices,
            long index_count,
            void* vertices,
            long vertex_count,
            long vertex_size,
            long vertex_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_filterIndexBufferMulti(
            uint[] destination,
            uint[] indices,
            long index_count,
            long vertex_count,
            MeshOptStream[] streams,
            long stream_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_generateShadowIndexBuffer(
            uint[] destination,
            uint[] indices,
            long index_count,
            void* vertices,
            long vertex_count,
            long vertex_size,
            long vertex_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_generateShadowIndexBufferMulti(
            uint[] destination,
            uint[] indices,
            long index_count,
            long vertex_count,
            MeshOptStream[] streams,
            long stream_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_generatePositionRemap(
            uint[] destination,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_generateAdjacencyIndexBuffer(
            uint[] destination,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_generateTessellationIndexBuffer(
            uint[] destination,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        #endregion

        #region GPU Optimization

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_optimizeVertexCache(
            uint[] destination,
            uint[] indices,
            long index_count,
            long vertex_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_optimizeVertexCacheStrip(
            uint[] destination,
            uint[] indices,
            long index_count,
            long vertex_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_optimizeVertexCacheFifo(
            uint[] destination,
            uint[] indices,
            long index_count,
            long vertex_count,
            uint cache_size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_optimizeOverdraw(
            uint[] destination,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            float threshold);

        // indices is input/output
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_optimizeVertexFetch(
            void* destination,
            uint[] indices,
            long index_count,
            void* vertices,
            long vertex_count,
            long vertex_size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_optimizeVertexFetchRemap(
            uint[] destination,
            uint[] indices,
            long index_count,
            long vertex_count);

        #endregion

        #region Simplification

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_simplify(
            uint[] destination,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            long target_index_count,
            float target_error,
            uint options,
            out float result_error);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_simplifyWithAttributes(
            uint[] destination,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            ref float vertex_attributes,
            long vertex_attributes_stride,
            float[] attribute_weights,
            long attribute_count,
            byte[]? vertex_lock,
            long target_index_count,
            float target_error,
            uint options,
            out float result_error);

        // Updates positions + attributes in-place.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_simplifyWithUpdate(
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            ref float vertex_attributes,
            long vertex_attributes_stride,
            float[] attribute_weights,
            long attribute_count,
            byte[]? vertex_lock,
            long target_index_count,
            float target_error,
            uint options,
            out float result_error);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_simplifySloppy(
            uint[] destination,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            byte[]? vertex_lock,
            long target_index_count,
            float target_error,
            out float result_error);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_simplifyPrune(
            uint[] destination,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            float target_error);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_simplifyPoints(
            uint[] destination,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            ref Vector3 vertex_colors,
            long vertex_colors_stride,
            float color_weight,
            long target_vertex_count);

        [DllImport(DllName, EntryPoint = "meshopt_simplifyPoints", CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_simplifyPointsNoColor(
            uint[] destination,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            void* vertex_colors,
            long vertex_colors_stride,
            float color_weight,
            long target_vertex_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float meshopt_simplifyScale(
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        #endregion

        #region Spatial Sort

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_spatialSortRemap(
            uint[] destination,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_spatialSortTriangles(
            uint[] destination,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_spatialClusterPoints(
            uint[] destination,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            long cluster_size);

        #endregion

        #region Stripify

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_stripify(
            uint[] destination,
            uint[] indices,
            long index_count,
            long vertex_count,
            uint restart_index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_stripifyBound(
            long index_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_unstripify(
            uint[] destination,
            uint[] indices,
            long index_count,
            uint restart_index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_unstripifyBound(
            long index_count);

        #endregion

        #region Analysis

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VertexCacheStatistics meshopt_analyzeVertexCache(
            uint[] indices,
            long index_count,
            long vertex_count,
            uint cache_size,
            uint warp_size,
            uint primgroup_size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VertexFetchStatistics meshopt_analyzeVertexFetch(
            uint[] indices,
            long index_count,
            long vertex_count,
            long vertex_size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern OverdrawStatistics meshopt_analyzeOverdraw(
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern CoverageStatistics meshopt_analyzeCoverage(
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        #endregion

        #region Meshlets / Bounds

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_buildMeshlets(
            Meshlet[] meshlets,
            uint[] meshlet_vertices,
            byte[] meshlet_triangles,
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            long max_vertices,
            long max_triangles,
            float cone_weight);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_buildMeshletsScan(
            Meshlet[] meshlets,
            uint[] meshlet_vertices,
            byte[] meshlet_triangles,
            uint[] indices,
            long index_count,
            long vertex_count,
            long max_vertices,
            long max_triangles);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_buildMeshletsBound(
            long index_count,
            long max_vertices,
            long max_triangles);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_optimizeMeshlet(
            uint[] meshlet_vertices,
            byte[] meshlet_triangles,
            long triangle_count,
            long vertex_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bounds meshopt_computeClusterBounds(
            uint[] indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bounds meshopt_computeMeshletBounds(
            uint[] meshlet_vertices,
            byte[] meshlet_triangles,
            long triangle_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bounds meshopt_computeSphereBounds(
            ref Vector3 positions,
            long count,
            long positions_stride,
            void* radii,
            long radii_stride);

        #endregion

        #region Tangents

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meshopt_generateTangents(
            float[] result,
            uint[]? indices,
            long index_count,
            ref Vector3 vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            ref Vector3 vertex_normals,
            long vertex_normals_stride,
            ref Vector2 vertex_uvs,
            long vertex_uvs_stride,
            uint options);

        #endregion

        #region Quantization

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort meshopt_quantizeHalf(float v);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float meshopt_dequantizeHalf(ushort h);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float meshopt_quantizeFloat(float v, int n);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int meshopt_computePositionExponent(
            ref Vector3 minv,
            ref Vector3 maxv,
            int min_exp,
            int max_bits);

        #endregion

        #region Generic Convenience Wrappers

        public static long GenerateVertexRemap<TVertex>(
            uint[] destination,
            uint[]? indices,
            long indexCount,
            TVertex[] vertices)
            where TVertex : unmanaged
        {
            fixed (TVertex* pVertices = vertices)
                return meshopt_generateVertexRemap(
                    destination,
                    indices,
                    indexCount,
                    pVertices,
                    vertices.Length,
                    sizeof(TVertex));
        }

        public static void RemapVertexBuffer<TVertex>(
            TVertex[] destination,
            TVertex[] vertices,
            long vertexCount,
            uint[] remap)
            where TVertex : unmanaged
        {
            fixed (TVertex* pDst = destination)
            fixed (TVertex* pSrc = vertices)
                meshopt_remapVertexBuffer(
                    pDst,
                    pSrc,
                    vertexCount,
                    sizeof(TVertex),
                    remap);
        }

        public static long OptimizeVertexFetch<TVertex>(
            TVertex[] destination,
            uint[] indices,
            long indexCount,
            TVertex[] vertices)
            where TVertex : unmanaged
        {
            fixed (TVertex* pDst = destination)
            fixed (TVertex* pSrc = vertices)
                return meshopt_optimizeVertexFetch(
                    pDst,
                    indices,
                    indexCount,
                    pSrc,
                    vertices.Length,
                    sizeof(TVertex));
        }

        public static long FilterIndexBufferByVertexPrefix<TVertex>(
            uint[] destination,
            uint[] indices,
            long indexCount,
            TVertex[] vertices,
            long vertexKeySize)
            where TVertex : unmanaged
        {
            fixed (TVertex* pVertices = vertices)
                return meshopt_filterIndexBuffer(
                    destination,
                    indices,
                    indexCount,
                    pVertices,
                    vertices.Length,
                    vertexKeySize,
                    sizeof(TVertex));
        }

        #endregion
    }
}