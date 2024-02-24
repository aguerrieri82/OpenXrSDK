#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace Xr.Engine.OpenGL
{
    public interface IGlVertexArray : IDisposable
    {
        void Update(object vertexSpan, object? indexSpan = null);

        void Draw(PrimitiveType primitive = PrimitiveType.Triangles);

        void Bind();

        void Unbind();

        Type VertexType { get; }

        Type IndexType { get; }
    }
}
