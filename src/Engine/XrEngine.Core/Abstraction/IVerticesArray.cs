
using System.Runtime.CompilerServices;

namespace XrEngine
{
    public interface IVerticesArray : IEnumerable<VertexData>
    {

        ref VertexData this[int index] { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        int Length { get; }

        Array ToArray();
    }

    public interface IVerticesList : IList<VertexData>
    {
    }
}
