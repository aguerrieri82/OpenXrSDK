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

        public static bool IsDepth(InternalFormat format)
        {
            return format == InternalFormat.DepthComponent ||
                   format == InternalFormat.DepthComponent16 ||
                   format == InternalFormat.DepthComponent16Arb ||
                   format == InternalFormat.DepthComponent16Oes ||
                   format == InternalFormat.DepthComponent16Sgix ||
                   format == InternalFormat.DepthComponent24 ||
                   format == InternalFormat.DepthComponent24Arb ||
                   format == InternalFormat.DepthComponent24Oes ||
                   format == InternalFormat.DepthComponent24Sgix ||
                   format == InternalFormat.DepthComponent32 ||
                   format == InternalFormat.DepthComponent32fNV ||
                   format == InternalFormat.DepthComponent32Oes ||
                   format == InternalFormat.DepthComponent32Sgix;
        }
    }
}
