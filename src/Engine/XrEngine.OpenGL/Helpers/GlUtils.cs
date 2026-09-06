#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif
using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public static class GlUtils
    {

#if GLES
        public const bool IsES = true;
#else
        public const bool IsES = false;
#endif

        public static int CalculateUnpackAlignment(uint width, uint bytesPerPixel)
        {
            var rowSize = width * bytesPerPixel;

            // OpenGL allows 1, 2, 4, or 8
            if (rowSize % 8 == 0) return 8;
            if (rowSize % 4 == 0) return 4;
            if (rowSize % 2 == 0) return 2;
            return 1;
        }

        public static void GetPixelFormat(TextureFormat format, out PixelFormat pixelFormat, out PixelType pixelType)
        {
            pixelFormat = format switch
            {
                TextureFormat.Depth32Float or
                TextureFormat.Depth16 or
                TextureFormat.Depth24 => PixelFormat.DepthComponent,

                TextureFormat.Depth32FloatStencil8 or
                TextureFormat.Depth24Stencil8 => PixelFormat.DepthStencil,

                TextureFormat.SRgba8 or
                TextureFormat.RgbaFloat32 or
                TextureFormat.RgbaFloat16 or
                TextureFormat.Rgba16 or
                TextureFormat.SRgbaInt16 or
                TextureFormat.Rgba8 => PixelFormat.Rgba,

                TextureFormat.SBgra8 or
                TextureFormat.Bgra8 => PixelFormat.Bgra,

                TextureFormat.GrayUInt32 => PixelFormat.RedInteger,

                TextureFormat.Gray8 or

                TextureFormat.Gray16 => PixelFormat.Red,

                TextureFormat.GrayInt16 => PixelFormat.RedInteger,

                TextureFormat.RgFloat32 or
                TextureFormat.RgFloat16 or

                TextureFormat.Rg8 => PixelFormat.RG,

                TextureFormat.RgUInt32 => PixelFormat.RGInteger,

                TextureFormat.RgbUInt32 => PixelFormat.RgbInteger,

                TextureFormat.RgbaUInt32 => PixelFormat.RgbaInteger,

                TextureFormat.GrayFloat32 or
                TextureFormat.GrayFloat16 => PixelFormat.Red,

                TextureFormat.Rgb8 or
                TextureFormat.RgbFloat16 or
                TextureFormat.RgbFloat32 or
                TextureFormat.Rgb9e5Float or
                TextureFormat.SRgb8 => PixelFormat.Rgb,

                _ => throw new NotSupportedException(),
            };

            pixelType = format switch
            {
                TextureFormat.Depth32Float or
                TextureFormat.RgbFloat32 or
                TextureFormat.RgbaFloat32 or
                TextureFormat.RgFloat32 or
                TextureFormat.RgFloat16 or
                TextureFormat.GrayFloat32 or
                TextureFormat.Rgb9e5Float or
                TextureFormat.GrayFloat16 => PixelType.Float,

                TextureFormat.Depth24 or
                TextureFormat.RgUInt32 or
                TextureFormat.RgbUInt32 or
                TextureFormat.RgbaUInt32 or
                TextureFormat.GrayUInt32 => PixelType.UnsignedInt,

                TextureFormat.RgbFloat16 or
                TextureFormat.RgbaFloat16 => PixelType.HalfFloat,

                TextureFormat.Depth16 => PixelType.UnsignedShort,
                TextureFormat.Depth24Stencil8 => PixelType.UnsignedInt248Oes,
                TextureFormat.Depth32FloatStencil8 => PixelType.Float32UnsignedInt248Rev,

                TextureFormat.Depth16 or
                TextureFormat.Gray16 or
                TextureFormat.Rgba16 or
                TextureFormat.SRgbaInt16 => PixelType.UnsignedShort,

                TextureFormat.GrayInt16 => PixelType.Short,

                TextureFormat.Rgba8 or
                TextureFormat.Bgra8 or
                TextureFormat.Gray8 or
                TextureFormat.Rgb8 or
                TextureFormat.SRgb8 or
                TextureFormat.SBgra8 or
                TextureFormat.Rg8 or
                TextureFormat.SRgba8 => PixelType.UnsignedByte,

                _ => throw new NotSupportedException(),
            };
        }

        public static InternalFormat ToInternalFormat(this TextureFormat format, TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed, uint blockSize = 0)
        {
            if (compression == TextureCompressionFormat.Uncompressed)
            {
                return format switch
                {
                    TextureFormat.Depth32Float => InternalFormat.DepthComponent32f,
                    TextureFormat.Depth24 => InternalFormat.DepthComponent24,
                    TextureFormat.Depth24Stencil8 => InternalFormat.Depth24Stencil8Oes,
                    TextureFormat.Depth32FloatStencil8 => InternalFormat.Depth32fStencil8,
                    TextureFormat.Depth16 => InternalFormat.DepthComponent16,

                    TextureFormat.SBgra8 or
                    TextureFormat.SRgba8 => InternalFormat.Srgb8Alpha8,

                    TextureFormat.Rgba8 or
                    TextureFormat.Bgra8 => InternalFormat.Rgba8,

                    TextureFormat.Gray8 => InternalFormat.R8,
                    TextureFormat.Gray16 => InternalFormat.R16,
                    TextureFormat.GrayInt16 => InternalFormat.R16i,
                    TextureFormat.GrayFloat16 => InternalFormat.R16f,
                    TextureFormat.GrayUInt32 => InternalFormat.R32ui,
                    TextureFormat.GrayFloat32 => InternalFormat.R32f,

                    TextureFormat.RgUInt32 => InternalFormat.RG32ui,
                    TextureFormat.RgFloat16 => InternalFormat.RG16f,
                    TextureFormat.RgFloat32 => InternalFormat.RG32f,
                    TextureFormat.Rg8 => InternalFormat.RG8,

                    TextureFormat.RgbUInt32 => InternalFormat.Rgb32ui,
                    TextureFormat.RgbFloat16 => InternalFormat.Rgb16f,
                    TextureFormat.RgbFloat32 => InternalFormat.Rgb32f,
                    TextureFormat.Rgb8 => InternalFormat.Rgb8,
                    TextureFormat.SRgb8 => InternalFormat.Srgb8,

                    TextureFormat.RgbaUInt32 => InternalFormat.Rgba32ui,
                    TextureFormat.RgbaFloat32 => InternalFormat.Rgba32f,
                    TextureFormat.RgbaFloat16 => InternalFormat.Rgba16f,
                    TextureFormat.Rgba16 => InternalFormat.Rgba16,

                    TextureFormat.Rgb9e5Float => InternalFormat.Rgb9E5,

                    _ => throw new NotSupportedException(),
                };
            }

            if (compression == TextureCompressionFormat.Etc2)
            {
                return format switch
                {
                    TextureFormat.Rgb8 => InternalFormat.CompressedRgb8Etc2,
                    TextureFormat.Rgba8 => InternalFormat.CompressedRgba8Etc2EacOes,
                    TextureFormat.SRgb8 => InternalFormat.CompressedSrgb8Etc2,
                    TextureFormat.SRgba8 => InternalFormat.CompressedSrgb8Alpha8Etc2EacOes,
                    _ => throw new NotSupportedException(format.ToString()),
                };
            }

            if (compression == TextureCompressionFormat.Astc)
            {
                if (format.IsSrgb())
                {
                    return blockSize switch
                    {
                        4 => InternalFormat.CompressedSrgb8Alpha8Astc4x4,
                        6 => InternalFormat.CompressedSrgb8Alpha8Astc6x6,
                        8 => InternalFormat.CompressedSrgb8Alpha8Astc8x8,
                        10 => InternalFormat.CompressedSrgb8Alpha8Astc10x10,
                        12 => InternalFormat.CompressedSrgb8Alpha8Astc12x12,
                        _ => throw new NotSupportedException(format.ToString()),
                    };
                }
                else
                {
                    return blockSize switch
                    {
                        3 => InternalFormat.CompressedRgbaAstc3x3x3Oes,
                        4 => InternalFormat.CompressedRgbaAstc4x4,
                        6 => InternalFormat.CompressedRgbaAstc6x6,
                        8 => InternalFormat.CompressedRgbaAstc8x8,
                        10 => InternalFormat.CompressedRgbaAstc10x10,
                        12 => InternalFormat.CompressedRgbaAstc12x12,
                        _ => throw new NotSupportedException(format.ToString()),
                    };
                }
            }

            if (compression == TextureCompressionFormat.Etc1)
            {
                return InternalFormat.Etc1Rgb8Oes;
            }

            if (compression == TextureCompressionFormat.Bc3)
            {
                return format switch
                {
                    TextureFormat.SRgb8 => InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext,
                    TextureFormat.Rgb8 => InternalFormat.CompressedRgbaS3TCDxt5Ext,
                    _ => throw new NotSupportedException(format.ToString()),
                };
            }
            if (compression == TextureCompressionFormat.Bc1)
            {
                return format switch
                {
                    TextureFormat.SRgb8 => InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext,
                    TextureFormat.Rgb8 => InternalFormat.CompressedRgbaS3TCDxt1Ext,
                    _ => throw new NotSupportedException(format.ToString()),
                };
            }
            if (compression == TextureCompressionFormat.Bc7)
            {
                return format switch
                {
                    TextureFormat.SRgb8 => InternalFormat.CompressedSrgbAlphaBptcUnormArb,
                    TextureFormat.Rgb8 => InternalFormat.CompressedRgbaBptcUnormArb,
                    _ => throw new NotSupportedException(format.ToString()),
                };
            }
            throw new NotSupportedException();
        }

        public static TextureFormat ToTextureFormat(this InternalFormat internalFormat)
        {
            return internalFormat switch
            {
                InternalFormat.Rgba16 => TextureFormat.Rgba16,
                InternalFormat.Rgb9E5 => TextureFormat.Rgb9e5Float,
                InternalFormat.Rgb32f => TextureFormat.RgbFloat32,
                InternalFormat.Rgba16f => TextureFormat.RgbaFloat16,
                InternalFormat.Rgba => TextureFormat.Rgba8,
                InternalFormat.Rgba8 => TextureFormat.Rgba8,
                InternalFormat.Srgb8Alpha8 => TextureFormat.SRgba8,
                InternalFormat.R16 => TextureFormat.Gray16,
                InternalFormat.RG16f => TextureFormat.RgbFloat16,
                InternalFormat.R32f => TextureFormat.GrayFloat32,
                InternalFormat.R16f => TextureFormat.GrayFloat16,
                //InternalFormat.DepthComponent16 => TextureFormat.GrayInt16,
                InternalFormat.R8 => TextureFormat.Gray8,
                InternalFormat.Depth24Stencil8 => TextureFormat.Depth24Stencil8,
                InternalFormat.DepthComponent24 => TextureFormat.Depth24,
                InternalFormat.Depth32fStencil8 => TextureFormat.Depth32FloatStencil8,
                InternalFormat.DepthComponent32f => TextureFormat.Depth32Float,
                InternalFormat.DepthComponent32 => TextureFormat.Depth32Float,
                InternalFormat.DepthComponent16 => TextureFormat.Depth16,
                InternalFormat.Rgb8 => TextureFormat.Rgb8,
                InternalFormat.RG32ui => TextureFormat.RgUInt32,
                InternalFormat.Rgb32ui => TextureFormat.RgbUInt32,
                InternalFormat.Rgba32ui => TextureFormat.RgbaUInt32,
                InternalFormat.R32ui => TextureFormat.GrayUInt32,
                InternalFormat.Srgb8 => TextureFormat.SRgb8,
                InternalFormat.Rgb16f => TextureFormat.RgbFloat16,
                InternalFormat.RG32f => TextureFormat.RgFloat32,

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

        public static bool HasDepth(this InternalFormat format)
        {
            return IsDepth(format) || IsDepthStencil(format);
        }

        public static bool IsSrgb(this InternalFormat format)
        {
            return format == InternalFormat.Srgb8Alpha8 ||
                   format == InternalFormat.Srgb ||
                   format == InternalFormat.Srgb8;
        }

        public static bool IsDepth(this InternalFormat format)
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
                   format == InternalFormat.DepthComponent32f ||
                   format == InternalFormat.DepthComponent32fNV ||
                   format == InternalFormat.DepthComponent32Oes ||
                   format == InternalFormat.DepthComponent32Sgix;
        }

        public static void EnsureRenderThread()
        {
            Debug.Assert(OpenGLRender.Current != null && Thread.CurrentThread == OpenGLRender.Current.Dispatcher.Thread);
        }
    }
}
