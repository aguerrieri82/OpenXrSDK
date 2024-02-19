#if GLES
using Silk.NET.OpenGLES;
#else
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
