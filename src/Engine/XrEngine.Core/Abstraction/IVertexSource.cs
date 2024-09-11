namespace XrEngine
{
    public enum DrawPrimitive
    {
        Triangle,
        Line,
        LineLoop
    }

    public interface IVertexSource
    {
        DrawPrimitive Primitive { get; }

        IReadOnlyList<Material> Materials { get; }

        VertexComponent ActiveComponents { get; }

        EngineObject Object { get; }

        void NotifyLoaded();
    }

    public interface IVertexSource<TVertices, TIndices> : IVertexSource
        where TVertices : unmanaged
        where TIndices : unmanaged
    {
        TIndices[] Indices { get; }

        TVertices[] Vertices { get; }
    }
}
