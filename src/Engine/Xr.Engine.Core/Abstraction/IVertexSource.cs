using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
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

    }

    public interface IVertexSource<TVertices, TIndices> : IVertexSource
        where TVertices : unmanaged 
        where TIndices : unmanaged
    {
        TIndices[] Indices { get; }

        TVertices[] Vertices { get; }
    }
}
