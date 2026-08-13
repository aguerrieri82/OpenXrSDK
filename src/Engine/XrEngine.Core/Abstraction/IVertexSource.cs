using SkiaSharp;

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
        Array Indices { get; }

        Array Vertices { get; }

        DrawPrimitive Primitive { get; }

        IReadOnlyList<Material> Materials { get; }

        VertexComponent ActiveComponents { get; }

        EngineObject Object { get; }

        int RenderPriority { get; }

        int InstanceCount => 1;

        void NotifyBuffers(IBuffer vertices, IBuffer? indices)
        {
        }

    }


}
