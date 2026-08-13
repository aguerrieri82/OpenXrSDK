using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace XrEngine
{
    public interface IGeometryVertices<T> where T : unmanaged, IVertexProvider
    {
        T[] Vertices { get; }
    }
}
