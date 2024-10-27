#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public static class GlUtils
    {
        public static TextureFormat GetTextureFormat(InternalFormat internalFormat)
        {
            return internalFormat switch
            {
                InternalFormat.Rgb32f => TextureFormat.RgbFloat32,
                InternalFormat.Rgba16f => TextureFormat.RgbaFloat16,
                InternalFormat.Rgba => TextureFormat.Rgba32,
                InternalFormat.Rgba8 => TextureFormat.Rgba32,
                InternalFormat.Srgb8Alpha8 => TextureFormat.SRgba32,
                InternalFormat.R16 => TextureFormat.Gray16,
                InternalFormat.DepthComponent16 => TextureFormat.Gray16,
                InternalFormat.R8 => TextureFormat.Gray8,
                InternalFormat.Depth24Stencil8 => TextureFormat.Depth24Stencil8,
                InternalFormat.DepthComponent24 => TextureFormat.Depth24Float,
                InternalFormat.Depth32fStencil8 => TextureFormat.Depth32Stencil8,
                InternalFormat.DepthComponent32f => TextureFormat.Depth32Float,
                InternalFormat.DepthComponent32 => TextureFormat.Depth32Float,
                _ => throw new NotSupportedException(),
            };
        }

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
