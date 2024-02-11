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

        public GlTexture2D(GL gl, uint handle)
            : base(gl)
        {
            Attach(handle);
        }

        public unsafe void Attach(uint handle)
        {
            _handle = handle;

            Bind();

            _gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int w);
            _gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int h);
            _gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureInternalFormat, out int intf);

            _gl.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureWrapS, out int ws);
            _gl.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureWrapT, out int wt);

            _gl.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureMinFilter, out int min);
            _gl.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureMagFilter, out int mag);

            _gl.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureBaseLevelSgis, out int bl);
            _gl.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureMaxLevelSgis, out int ml);

            _width = (uint)w;
            _height = (uint)h;

            WrapS = (TextureWrapMode)ws;
            WrapT = (TextureWrapMode)wt;
            MinFilter = (TextureMinFilter)min;
            MagFilter = (TextureMagFilter)mag;
            BaseLevel = (uint)bl;
            MaxLevel = (uint)ml;
            InternalFormat = (InternalFormat)intf;
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

                _gl.TexImage2D(
                     TextureTarget.Texture2D,
                     0,
                     internalFormat,
                     width,
                     height,
                     0,
                     pixelFormat,
                     pixelType,
                     data);
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
                     TextureTarget.Texture2D,
                     0,
                     internalFormat,
                     width,
                     height,
                     0,
                     dataSize,
                     data);
            }

            InternalFormat = internalFormat;

            Update();
        }

        public void Update()
        {
            Bind();

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)WrapS);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)WrapT);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)MinFilter);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)MagFilter);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, BaseLevel);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, MaxLevel);
            //_gl.GenerateMipmap(TextureTarget.Texture2D);

            Unbind();
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            //_gl.ActiveTexture(textureSlot);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Unbind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        public override void Dispose()
        {
            _gl.DeleteTexture(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }

        public InternalFormat InternalFormat;

        public TextureWrapMode WrapS;

        public TextureWrapMode WrapT;

        public TextureMinFilter MinFilter;

        public TextureMagFilter MagFilter;

        public uint BaseLevel;

        public uint MaxLevel;

        public uint Width => _width;

        public uint Height => _height;
    }
}
