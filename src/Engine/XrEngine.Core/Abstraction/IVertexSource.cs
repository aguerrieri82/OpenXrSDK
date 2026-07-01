using XrEngine;

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

        EngineObject Object { get; }

        int RenderPriority { get; }

        int InstanceCount => 1;


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
