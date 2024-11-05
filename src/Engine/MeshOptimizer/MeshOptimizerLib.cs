using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshOptimizer
{
    public static unsafe class MeshOptimizerLib
    {


        [DllImport("meshoptimizer-native")]
        public static extern long meshopt_simplifyWithAttributes(
            uint[] destination,
            uint[] indices,
            long index_count,
            float* vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            float* vertex_attributes,
            long vertex_attributes_stride,
            float[] attribute_weights,
            long attribute_count,
            byte[]? vertex_lock,
            long target_index_count,
            float target_error,
            uint options,
            out float result_error);
    

        [DllImport("meshoptimizer-native", CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_generateVertexRemap(
           uint[] destination,
           uint[]? indices,
           long index_count,
           void* vertices,
           long vertex_count,
           long vertex_size);

        [DllImport("meshoptimizer-native", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool meshopt_optimizeVertexCache(
           uint[] destination,
           uint[] indices,
           long index_count,
           long vertex_count);

        [DllImport("meshoptimizer-native", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool meshopt_optimizeOverdraw(
           uint[] destination,
           uint[] indices,
           long index_count,
           float* vertex_positions,
           long vertex_count,
           long vertex_positions_stride,
           float threshold);



        [DllImport("meshoptimizer-native", CallingConvention = CallingConvention.Cdecl)]
        public static extern long meshopt_optimizeVertexFetch(
           void* destination,
           uint[] indices,
           long index_count,
           void* vertices,
           long vertex_count,
           long vertex_size);
    }
}
