using System.Numerics;

namespace XrEngine
{
    public interface ICompressedVertexSource : IVertexSource
    {
        Matrix4x4 VerticesRemap { get; }

        Type? CompVertexType { get; }

        Type? CompIndexType { get; }

        unsafe void CompressVertices(void* pSrc, void* pDst, int count);

        unsafe void CompressIndices(void* pSrc, void* pDst, int count);
    }
}
