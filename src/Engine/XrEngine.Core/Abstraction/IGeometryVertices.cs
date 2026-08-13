
namespace XrEngine
{
    public interface IVerticesArray : IEnumerable<VertexData>
    {
        ref VertexData this[int index] { get; }

        int Length { get; }

        Array ToArray();
    }

    public interface IVerticesList : IList<VertexData>
    {

    }


    public interface IGeometryVertices
    {
        IVerticesArray Vertices { get; }
    }

    public interface IGeometryVertices<TVert> : IGeometryVertices
        where TVert : unmanaged, IVertexProvider
    {
        new TVert[] Vertices { get; set; }
    }
}
