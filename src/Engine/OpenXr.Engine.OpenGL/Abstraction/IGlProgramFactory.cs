#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenXr.Engine.OpenGL
{
    public interface IGlProgramFactory
    {
        GlProgram CreateProgram(GL gl, string vSource, string fSource, GlRenderOptions options);
    }
}
