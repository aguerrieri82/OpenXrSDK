using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace XrEngine
{
    public interface IVertexProvider
    {
        ref VertexData Vertex { [MethodImpl(MethodImplOptions.AggressiveInlining)][UnscopedRef] get; }
    }
}
