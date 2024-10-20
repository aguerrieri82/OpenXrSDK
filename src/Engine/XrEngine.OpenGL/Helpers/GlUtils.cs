#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public static class GlUtils
    {

        public static bool IsDepthStencil(InternalFormat format)
        {
            return format == InternalFormat.Depth24Stencil8 ||
                   format == InternalFormat.Depth24Stencil8Ext ||
                   format == InternalFormat.Depth24Stencil8Oes ||
                   format == InternalFormat.Depth32fStencil8 ||
                   format == InternalFormat.Depth32fStencil8NV;
        }
    }
}
