#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace OpenXr.Engine.OpenGL
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
            MaxLevel = 8;
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

            Bind();

            _gl.GetTexLevelParameter(Target, 0, GetTextureParameter.TextureWidth, out int w);
            _gl.GetTexLevelParameter(Target, 0, GetTextureParameter.TextureHeight, out int h);
            _gl.GetTexLevelParameter(Target, 0, GetTextureParameter.TextureInternalFormat, out int intf);

            if (SampleCount <= 1)
            {
                _gl.GetTexParameter(Target, GetTextureParameter.TextureWrapS, out int ws);
                _gl.GetTexParameter(Target, GetTextureParameter.TextureWrapT, out int wt);

                _gl.GetTexParameter(Target, GetTextureParameter.TextureMinFilter, out int min);
                _gl.GetTexParameter(Target, GetTextureParameter.TextureMagFilter, out int mag);

                WrapS = (TextureWrapMode)ws;
                WrapT = (TextureWrapMode)wt;
                MinFilter = (TextureMinFilter)min;
                MagFilter = (TextureMagFilter)mag;
            }

            _gl.GetTexParameter(Target, GetTextureParameter.TextureBaseLevelSgis, out int bl);
            _gl.GetTexParameter(Target, GetTextureParameter.TextureMaxLevelSgis, out int ml);

            _width = (uint)w;
            _height = (uint)h;
            _internalFormat = (InternalFormat)intf;


            BaseLevel = (uint)bl;
            MaxLevel = (uint)ml;

            Unbind();
        }

        public unsafe void Create(uint width, uint height, TextureFormat format, TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed, void* data = null, uint dataSize = 0)
        {
            Dispose();

            _handle = _gl.GenTexture();
            _width = width;
            _height = height;

            Bind();

            InternalFormat internalFormat;
            PixelFormat pixelFormat;
            PixelType pixelType;

            if (compression == TextureCompressionFormat.Uncompressed)
            {
                switch (format)
                {
                    case TextureFormat.Depth32Float:
                        internalFormat = InternalFormat.DepthComponent32;
                        break;
                    case TextureFormat.Depth24Float:
                        internalFormat = InternalFormat.DepthComponent24;
                        break;
                    case TextureFormat.Rgba32:
                    case TextureFormat.Bgra32:
                        internalFormat = InternalFormat.Rgb8;
                        break;
                    default:
                        throw new NotSupportedException();
                }

                switch (format)
                {
                    case TextureFormat.Depth32Float:
                    case TextureFormat.Depth24Float:
                        pixelFormat = PixelFormat.DepthComponent;
                        break;
                    case TextureFormat.Rgba32:
                        pixelFormat = PixelFormat.Rgba;
                        break;
                    case TextureFormat.Bgra32:
                        pixelFormat = PixelFormat.Bgra;
                        break;
                    default:
                        throw new NotSupportedException();
                }

                switch (format)
                {
                    case TextureFormat.Depth32Float:
                    case TextureFormat.Depth24Float:
                        pixelType = PixelType.Float;
                        break;
                    case TextureFormat.Rgba32:
                    case TextureFormat.Bgra32:
                        pixelType = PixelType.UnsignedByte;
                        break;
                    default:
                        throw new NotSupportedException();
                }

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
                        _gl.TexImage2D(
                           Target,
                           0,
                           internalFormat,
                           width,
                           height,
                           0,
                           pixelFormat,
                           pixelType,
                           data);
                    }
                }
            }
            else
            {
                if (compression == TextureCompressionFormat.Etc2)
                {
                    switch (format)
                    {
                        case TextureFormat.Rgb24:
                            internalFormat = InternalFormat.CompressedRgb8Etc2;
                            break;
                        case TextureFormat.SRgb24:
                            internalFormat = InternalFormat.CompressedSrgb8Etc2;
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }
                else
                    throw new NotSupportedException();

                _gl.PixelStore(PixelStoreParameter.PackAlignment, 1);

                _gl.CompressedTexImage2D(
                     Target,
                     0,
                     internalFormat,
                     width,
                     height,
                     0,
                     dataSize,
                     data);

                MinFilter = TextureMinFilter.Linear;
                MagFilter = TextureMagFilter.Linear;

                _isCompressed = true;
            }

            _internalFormat = internalFormat;

            Update();
        }

        public void Update()
        {
            Bind();

            if (SampleCount <= 1)
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

                if (!_isCompressed)
                    _gl.GenerateMipmap(Target);
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
            _gl.DeleteTexture(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }


        public TextureWrapMode WrapS;

        public TextureWrapMode WrapT;

        public TextureMinFilter MinFilter;

        public TextureMagFilter MagFilter;

        public uint SampleCount;

        public uint BaseLevel;

        public uint MaxLevel;

        internal bool IsDepth => _internalFormat >= InternalFormat.DepthComponent16 && _internalFormat <= InternalFormat.DepthComponent32Sgix;

        internal TextureTarget Target => SampleCount > 1 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D;

        public InternalFormat InternalFormat => _internalFormat;

        public bool IsCompressed => _isCompressed;

        public uint Width => _width;

        public uint Height => _height;
    }
}
