using System.Runtime.InteropServices;

namespace MeshOptimizer
{
    public static unsafe class MeshOptimizerLib
    {
        [DllImport("meshoptimizer")]
        public static extern long meshopt_simplifyWithAttributes(
            [MarshalAs(UnmanagedType.LPArray)] uint[] destination,
            [MarshalAs(UnmanagedType.LPArray)] uint[] indices,
            long index_count,
            float* vertex_positions,
            long vertex_count,
            long vertex_positions_stride,
            float* vertex_attributes,
            long vertex_attributes_stride,
            [MarshalAs(UnmanagedType.LPArray)] float[] attribute_weights,
            long attribute_count,
            long target_index_count,
            float target_error,
            uint options,
            out float result_error);

    }
}
