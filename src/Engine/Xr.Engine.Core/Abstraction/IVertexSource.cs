namespace Xr.Engine
{
    public enum DrawPrimitive
    {
        Triangle,
        Line
    }

    public interface IVertexSource
    {
        DrawPrimitive Primitive { get; }

        IList<Material> Materials { get; }

        VertexComponent ActiveComponents { get; }

        EngineObject Object { get; }

    }

    public interface IVertexSource<TVertices, TIndices> : IVertexSource
        where TVertices : unmanaged
        where TIndices : unmanaged
    {
        TIndices[] Indices { get; }

        TVertices[] Vertices { get; }
    }
}
