#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.Maths;
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public unsafe class GlWrapper : Wrapper<GL>
    {
        public GlWrapper(GL instance) : base(instance)
        {
        }
    }
}