#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public class GlTexture2D : GlObject
    {
        protected uint _width;
        protected uint _height;
        protected bool _isCompressed;
        protected InternalFormat _internalFormat;

        public GlTexture2D(GL gl)
            : base(gl)
        {
            WrapS = TextureWrapMode.ClampToEdge;
            WrapT = TextureWrapMode.ClampToEdge;
            MinFilter = TextureMinFilter.LinearMipmapLinear;
            MagFilter = TextureMagFilter.Linear;
            BaseLevel = 0;
            MaxLevel = 16;
            Target = TextureTarget.Texture2D;

            _handle = _gl.GenTexture();
        }

        public GlTexture2D(GL gl, uint handle, uint sampleCount = 1)
            : base(gl)
        {
            SampleCount = sampleCount;
            Attach(handle);
        }


        public unsafe void Attach(uint handle)
        {
            _handle = handle;

            Target = _gl.GetTexture2DTarget(handle);

            Bind();

            bool isMultiSample = Target == TextureTarget.Texture2DMultisample || Target == TextureTarget.Texture2DMultisampleArray;

            _gl.GetTexLevelParameter(Target, 0, GetTextureParameter.TextureWidth, out int w);
            _width = (uint)w;

            _gl.GetTexLevelParameter(Target, 0, GetTextureParameter.TextureHeight, out int h);
            _height = (uint)h;

            _gl.GetTexLevelParameter(Target, 1, GetTextureParameter.TextureInternalFormat, out int intf);
            _internalFormat = (InternalFormat)intf;

            if (isMultiSample)
            {
                _gl.GetTexLevelParameter(Target, 1, GLEnum.TextureSamples, out int sc);
                SampleCount = (uint)sc;
            }
            else
            {
                _gl.GetTexParameter(Target, GetTextureParameter.TextureWrapS, out int ws);
                WrapS = (TextureWrapMode)ws;

                _gl.GetTexParameter(Target, GetTextureParameter.TextureWrapT, out int wt);
                WrapT = (TextureWrapMode)wt;

                _gl.GetTexParameter(Target, GetTextureParameter.TextureMinFilter, out int min);
                MinFilter = (TextureMinFilter)min;

                _gl.GetTexParameter(Target, GetTextureParameter.TextureMagFilter, out int mag);
                MagFilter = (TextureMagFilter)mag;
            }

            _gl.GetTexParameter(Target, GetTextureParameter.TextureBaseLevelSgis, out int bl);
            BaseLevel = (uint)bl;

            _gl.GetTexParameter(Target, GetTextureParameter.TextureMaxLevelSgis, out int ml);
            MaxLevel = (uint)ml;

            Unbind();
        }

        public unsafe void Update(Texture2D texture2D)
        {
            MinFilter = (TextureMinFilter)texture2D.MinFilter;
            MagFilter = (TextureMagFilter)texture2D.MagFilter;
            WrapS = (TextureWrapMode)texture2D.WrapS;
            WrapT = (TextureWrapMode)texture2D.WrapT;

            Update(texture2D.Width, texture2D.Height, texture2D.Format, texture2D.Compression, texture2D.Data);
        }

        public unsafe void Update(uint width, uint height, TextureFormat format, TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed, IList<TextureData>? data = null)
        {

            _width = width;
            _height = height;

            Bind();

            InternalFormat internalFormat;
            PixelFormat pixelFormat;
            PixelType pixelType;

            if (compression == TextureCompressionFormat.Uncompressed)
            {
                internalFormat = format switch
                {
                    TextureFormat.Depth32Float => InternalFormat.DepthComponent32,
                    TextureFormat.Depth24Float => InternalFormat.DepthComponent24,
                    TextureFormat.Depth24Stencil8 => InternalFormat.Depth24Stencil8Oes,

                    TextureFormat.SBgra32 or
                    TextureFormat.SRgba32 => InternalFormat.Srgb8Alpha8,

                    TextureFormat.Rgba32 or
                    TextureFormat.Bgra32 => InternalFormat.Rgba8,

                    TextureFormat.Gray8 => InternalFormat.R8,

                    _ => throw new NotSupportedException(),
                };

                pixelFormat = format switch
                {
                    TextureFormat.Depth32Float or
                    TextureFormat.Depth24Float => PixelFormat.DepthComponent,

                    TextureFormat.Depth24Stencil8 => PixelFormat.DepthStencil,

                    TextureFormat.SRgba32 or
                    TextureFormat.Rgba32 => PixelFormat.Rgba,

                    TextureFormat.SBgra32 or
                    TextureFormat.Bgra32 => PixelFormat.Bgra,

                    TextureFormat.Gray8 => PixelFormat.Red,

                    _ => throw new NotSupportedException(),
                };

                pixelType = format switch
                {
                    TextureFormat.Depth32Float or
                    TextureFormat.Depth24Float => PixelType.Float,

                    TextureFormat.Depth24Stencil8 => PixelType.UnsignedInt248Oes,

                    TextureFormat.Rgba32 or
                    TextureFormat.Bgra32 or
                    TextureFormat.Gray8 or
                    TextureFormat.SRgb24 or
                    TextureFormat.SBgra32 or
                    TextureFormat.SRgba32 => PixelType.UnsignedByte,

                    _ => throw new NotSupportedException(),
                };

                if (SampleCount > 1)
                {
                    _gl.TexStorage2DMultisample(
                         Target,
                         SampleCount,
                         (SizedInternalFormat)internalFormat,
                         width,
                         height,
                         true);
                }
                else
                {
                    if (pixelFormat == PixelFormat.DepthComponent)
                    {
                        _gl.TexStorage2D(Target,
                            1,
                            (SizedInternalFormat)internalFormat,
                            width,
                            height);
                    }
                    else
                    {

                        if (data != null && data.Count > 0)
                        {
                            foreach (var level in data)
                            {
                                fixed (byte* pData = level.Data)
                                {

                                    _gl.TexImage2D(
                                        Target,
                                        (int)level.MipLevel,
                                        internalFormat,
                                        level.Width,
                                        level.Height,
                                        0,
                                        pixelFormat,
                                        pixelType,
                                        pData);
                                }
                            }
                        }
                        else
                        {
                            _gl.TexImage2D(
                                Target,
                                0,
                                internalFormat,
                                width,
                                height,
                                0,
                                pixelFormat,
                                pixelType,
                                null);
                        }
                    }
                }
            }
            else
            {
                if (compression == TextureCompressionFormat.Etc2)
                {
                    internalFormat = format switch
                    {
                        TextureFormat.Rgb24 => InternalFormat.CompressedRgb8Etc2,
                        TextureFormat.Rgba32 => InternalFormat.CompressedRgba8Etc2Eac,
                        TextureFormat.SRgb24 => InternalFormat.CompressedSrgb8Etc2,
                        TextureFormat.SRgba32 => InternalFormat.CompressedSrgb8Alpha8Etc2Eac,
                        _ => throw new NotSupportedException(format.ToString()),
                    };
                }
                else if (compression == TextureCompressionFormat.Etc1)
                {
                    internalFormat = InternalFormat.Etc1Rgb8Oes;
                }
                else
                    throw new NotSupportedException();

                Debug.Assert(data != null);

                foreach (var level in data)
                {
                    fixed (byte* pData = level.Data)
                    {
                        _gl.CompressedTexImage2D(
                            Target,
                            (int)level.MipLevel,
                            internalFormat,
                            level.Width,
                            level.Height,
                            0,
                            (uint)level.Data!.Length,
                            pData);
                    }
                }

                _isCompressed = true;
            }

            _internalFormat = internalFormat;

            if (data != null)
            {
                if (data.Count == 1)
                {
                    if (MinFilter == TextureMinFilter.LinearMipmapLinear)
                    {
                        if (!_isCompressed)
                        {
                            _gl.GenerateMipmap(Target);
                            _gl.GetTexParameter(Target, GetTextureParameter.TextureMaxLevelSgis, out int ml);
                            MaxLevel = (uint)ml;
                        }
                        else
                            MinFilter = TextureMinFilter.Linear;
                    }
                }
                else
                {
                    MaxLevel = data[data.Count - 1].MipLevel;
                    MinFilter = TextureMinFilter.LinearMipmapLinear;
                }
            }

            Update();
        }


        public void Update()
        {
            Bind();

            bool isMultiSample = Target == TextureTarget.Texture2DMultisample || Target == TextureTarget.Texture2DMultisampleArray;

            if (!isMultiSample)
            {
                _gl.TexParameter(Target, TextureParameterName.TextureWrapS, (int)WrapS);
                _gl.TexParameter(Target, TextureParameterName.TextureWrapT, (int)WrapT);
                _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)MinFilter);
                _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)MagFilter);
            }

            if (!IsDepth)
            {
                _gl.TexParameter(Target, TextureParameterName.TextureBaseLevel, BaseLevel);
                _gl.TexParameter(Target, TextureParameterName.TextureMaxLevel, MaxLevel);
            }

            Unbind();
        }

        public void Bind()
        {
            _gl.BindTexture(Target, _handle);
        }

        public void Unbind()
        {
            _gl.BindTexture(Target, 0);
        }

        public override void Dispose()
        {
            if (_handle != 0)
            {
                _gl.DeleteTexture(_handle);
                _handle = 0;
            }
            GC.SuppressFinalize(this);
        }

        public TextureWrapMode WrapS;

        public TextureWrapMode WrapT;

        public TextureMinFilter MinFilter;

        public TextureMagFilter MagFilter;

        public uint SampleCount;

        public uint BaseLevel;

        public uint MaxLevel;

        public TextureTarget Target;

        public InternalFormat InternalFormat => _internalFormat;

        public bool IsCompressed => _isCompressed;

        public uint Width => _width;

        public uint Height => _height;

        public bool IsDepth => _internalFormat >= InternalFormat.DepthComponent16 && _internalFormat <= InternalFormat.DepthComponent32Sgix;
    }
}
