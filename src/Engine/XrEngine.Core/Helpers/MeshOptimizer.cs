using MeshOptimizer;

namespace XrEngine
{
    public static class MeshOptimizer
    {
        public unsafe static uint[] GenerateVertexRemap(Geometry3D geometry)
        {
            var indexCount = geometry.Indices.Length == 0 ? geometry.Vertices.Length : geometry.Indices.Length;
            var remap = new uint[indexCount];
            fixed (VertexData* pData = geometry.Vertices)
            {
                var total = MeshOptimizerLib.meshopt_generateVertexRemap(
                    remap,
                    geometry.Indices,
                    geometry.Indices.Length,
                    &pData->Pos,
                    geometry.Vertices.Length,
                    sizeof(VertexData));

                Array.Resize(ref remap, (int)total);
            }
            return remap;
        }

        public static unsafe void OptimizeOverdraw(Geometry3D geometry, float threshold)
        {
            var indexCount = geometry.Indices.Length;
            var result = new uint[indexCount];

            fixed (VertexData* pData = geometry.Vertices)
            {
                MeshOptimizerLib.meshopt_optimizeOverdraw(
                  result,
                  geometry.Indices,
                  geometry.Indices.Length,
                  &pData->Pos.X,
                  geometry.Vertices.Length,
                  sizeof(VertexData),
                 threshold);
            }
            geometry.Indices = result;
        }


        public static unsafe void OptimizeVertexFetch(Geometry3D geometry)
        {
            var indexCount = geometry.Indices.Length;
            var result = new VertexData[geometry.Vertices.Length];

            fixed (VertexData* pData = geometry.Vertices)
            fixed (VertexData* pResult = result)
            {
                var count = MeshOptimizerLib.meshopt_optimizeVertexFetch(
                  pResult,
                  geometry.Indices,
                  geometry.Indices.Length,
                  &pData->Pos.X,
                  geometry.Vertices.Length,
                  sizeof(VertexData));

                Array.Resize(ref result, (int)count);
                geometry.Vertices = result;
            }

        }


        public static void OptimizeVertexCache(Geometry3D geometry)
        {
            var indexCount = geometry.Indices.Length;
            var result = new uint[indexCount];

            MeshOptimizerLib.meshopt_optimizeVertexCache(
                  result,
                  geometry.Indices,
                  geometry.Indices.Length,
                  geometry.Vertices.Length);

            geometry.Indices = result;
        }


        public unsafe static void Simplify(Geometry3D geometry, float targetIndicesFactor = 0.5f, float targetError = 0.01f)
        {
            var targetIndices = (uint)(geometry.Indices!.Length * targetIndicesFactor);

            geometry.EnsureIndices();

            var dest = new uint[geometry.Indices!.Length];

            var weights = new float[5];
            for (var i = 0; i < 5; i++)
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
                     &pVert->Normal.X,
                     sizeof(VertexData),
                     weights,
                     weights.Length,
                     null,
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
