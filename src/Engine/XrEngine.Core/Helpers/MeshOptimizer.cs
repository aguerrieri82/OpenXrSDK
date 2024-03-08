using MeshOptimizer;

namespace XrEngine
{
    public static class MeshOptimizer
    {
        public unsafe static void Simplify(Geometry3D geometry, float targetError = 0.01f)
        {
            var targetIndices = (uint)(geometry.Indices!.Length * 0.1f);

            geometry.EnsureIndices();

            var dest = new uint[geometry.Indices!.Length];

            var weights = new float[2];
            for (var i = 0; i < 2; i++)
                weights[i] = 0.1f;

            fixed (VertexData* pVert = geometry.Vertices)
            {
                var count = MeshOptimizerLib.meshopt_simplifyWithAttributes(
                     dest,
                     geometry.Indices,
                     geometry.Indices.Length,
                     (float*)pVert,
                     geometry.Vertices!.Length,
                     sizeof(VertexData),
                     (float*)(pVert + 24),
                     sizeof(VertexData),
                     weights,
                     2,
                     targetIndices,
                     targetError,
                     0,
                     out var error);

                Array.Resize(ref dest, (int)count);
            }

            geometry.Indices = dest;

        }
    }
}
