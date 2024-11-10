#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public static class GlUtils
    {
        public static uint GetPixelSize(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Rg88 => 16,
                TextureFormat.Rgba32 => 32,
                TextureFormat.Rgb24 => 24,
                TextureFormat.SRgb24 => 24,
                TextureFormat.RgbFloat32 => 32 * 3,
                TextureFormat.RgbaFloat32 => 32 * 4,
                TextureFormat.RgbaFloat16 => 16 * 4,
                _ => throw new NotSupportedException()
            };

        }

        public static void GetPixelFormat(TextureFormat format, out PixelFormat pixelFormat, out PixelType pixelType)
        {
            pixelFormat = format switch
            {
                TextureFormat.Depth32Float or
                TextureFormat.Depth24Float => PixelFormat.DepthComponent,

                TextureFormat.Depth32Stencil8 or
                TextureFormat.Depth24Stencil8 => PixelFormat.DepthStencil,

                TextureFormat.SRgba32 or
                TextureFormat.RgbaFloat32 or
                TextureFormat.RgbaFloat16 or
                TextureFormat.Rgba32 => PixelFormat.Rgba,

                TextureFormat.SBgra32 or
                TextureFormat.Bgra32 => PixelFormat.Bgra,

                TextureFormat.Gray8 or
                TextureFormat.Gray16 => PixelFormat.Red,

                TextureFormat.RgFloat32 or
                TextureFormat.Rg88 => PixelFormat.RG,

                TextureFormat.RFloat32 => PixelFormat.Red,

                TextureFormat.Rgb24 or
                TextureFormat.RgbFloat16 or
                TextureFormat.RgbFloat32 or
                TextureFormat.SRgb24 => PixelFormat.Rgb,

                _ => throw new NotSupportedException(),
            };

            pixelType = format switch
            {
                TextureFormat.Depth32Float or
                TextureFormat.RgbFloat32 or
                TextureFormat.RgbaFloat32 or
                TextureFormat.RgFloat32 or
                TextureFormat.RFloat32 or
                TextureFormat.Depth24Float => PixelType.Float,

                TextureFormat.RgbFloat16 => PixelType.HalfFloat,
                TextureFormat.RgbaFloat16 => PixelType.HalfFloat,

                TextureFormat.Depth24Stencil8 => PixelType.UnsignedInt248Oes,

                TextureFormat.Depth32Stencil8 => PixelType.Float32UnsignedInt248Rev,

                TextureFormat.Gray16 => PixelType.UnsignedShort,

                TextureFormat.Rgba32 or
                TextureFormat.Bgra32 or
                TextureFormat.Gray8 or
                TextureFormat.Rgb24 or
                TextureFormat.SRgb24 or
                TextureFormat.SBgra32 or
                TextureFormat.Rg88 or
                TextureFormat.SRgba32 => PixelType.UnsignedByte,

                _ => throw new NotSupportedException(),
            };
        }

        public static InternalFormat GetInternalFormat(TextureFormat format, TextureCompressionFormat compression)
        {

            if (compression == TextureCompressionFormat.Uncompressed)
            {
                return format switch
                {
                    TextureFormat.Depth32Float => InternalFormat.DepthComponent32f,
                    TextureFormat.Depth24Float => InternalFormat.DepthComponent24,
                    TextureFormat.Depth24Stencil8 => InternalFormat.Depth24Stencil8Oes,
                    TextureFormat.Depth32Stencil8 => InternalFormat.Depth32fStencil8,

                    TextureFormat.SBgra32 or
                    TextureFormat.SRgba32 => InternalFormat.Srgb8Alpha8,

                    TextureFormat.Rgba32 or
                    TextureFormat.Bgra32 => InternalFormat.Rgba8,

                    TextureFormat.Gray8 => InternalFormat.R8,
                    TextureFormat.Gray16 => InternalFormat.R16,

                    TextureFormat.RgbFloat32 => InternalFormat.Rgb32f,

                    TextureFormat.RgbaFloat32 => InternalFormat.Rgba32f,

                    TextureFormat.RgbFloat16 => InternalFormat.Rgb16f,

                    TextureFormat.RgbaFloat16 => InternalFormat.Rgba16f,

                    TextureFormat.RgFloat32 => InternalFormat.RG32f,

                    TextureFormat.RFloat32 => InternalFormat.R32f,

                    TextureFormat.Rgb24 => InternalFormat.Rgb8,

                    TextureFormat.Rg88 => InternalFormat.RG8,

                    _ => throw new NotSupportedException(),
                };
            }

            if (compression == TextureCompressionFormat.Etc2)
            {

                return format switch
                {
                    TextureFormat.Rgb24 => InternalFormat.CompressedRgb8Etc2,
                    TextureFormat.Rgba32 => InternalFormat.CompressedRgba8Etc2EacOes,
                    TextureFormat.SRgb24 => InternalFormat.CompressedSrgb8Etc2,
                    TextureFormat.SRgba32 => InternalFormat.CompressedSrgb8Alpha8Etc2EacOes,
                    _ => throw new NotSupportedException(format.ToString()),
                };
            }

            if (compression == TextureCompressionFormat.Etc1)
            {
                return InternalFormat.Etc1Rgb8Oes;
            }

            throw new NotSupportedException();
        }

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
