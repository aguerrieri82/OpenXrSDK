using System.Numerics;

namespace XrEngine
{
    public enum DrawPrimitive
    {
        Triangle,
        Line,
        LineLoop,
        Point,
        Patch,
        Quad
    }

    public interface IVertexSource : ILayer3DItem, IGpuObject
    {
        DrawPrimitive Primitive { get; }

        IReadOnlyList<Material> Materials { get; }

        VertexComponent ActiveComponents { get; }

        EngineObject Host { get; }

        int RenderPriority { get; }

        int InstanceCount => 1;

    }

    public interface ICompressedVertexSource : IVertexSource
    {
        Matrix4x4 VerticesRemap { get; }

        Type? CompVertexType { get; }

        Type? CompIndexType { get; }

        unsafe void CompressVertices(void* pSrc, void* pDst, int count);

        unsafe void CompressIndices(void* pSrc, void* pDst, int count);
    }

    public interface IVertexSource<TVertices, TIndices> : IVertexSource
        where TVertices : unmanaged
        where TIndices : unmanaged
    {
        TIndices[] Indices { get; }

        TVertices[] Vertices { get; }

        void NotifyBuffers(IBuffer<TVertices> vertices, IBuffer<TIndices>? indices)
        {

        }
    }
}
