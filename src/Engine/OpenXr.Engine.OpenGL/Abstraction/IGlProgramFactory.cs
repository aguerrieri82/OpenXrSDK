using Silk.NET.OpenGLES;

namespace OpenXr.Engine.OpenGL
{
    public interface IGlProgramFactory
    {
        GlProgram CreateProgram(GL gl, string vSource, string fSource, GlRenderOptions options);
    }
}
