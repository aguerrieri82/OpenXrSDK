using System.Diagnostics.CodeAnalysis;

namespace XrEngine
{
    public struct SkinnedVertexData : IVertexProvider
    {
        public VertexData Vertex;

        public SkinData Skin;

        [UnscopedRef]
        ref VertexData IVertexProvider.Vertex => ref Vertex;
    }
}
