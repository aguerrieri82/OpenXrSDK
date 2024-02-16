#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenXr.Engine.OpenGL

{
    public class GlShaderCache
    {
        protected Dictionary<string, string> _sources = [];

        protected Dictionary<string, uint> _shaders = [];

        protected Dictionary<string, uint> _programs = [];
    }
}
