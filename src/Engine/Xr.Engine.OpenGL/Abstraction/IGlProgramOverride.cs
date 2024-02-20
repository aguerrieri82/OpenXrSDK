#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenXr.Engine.OpenGL
{
    public interface IGlProgramOverride 
    {
        void BeginEdit(GlProgram program);

        bool SetCamera(GlProgram program, Camera camera);
    }
}
