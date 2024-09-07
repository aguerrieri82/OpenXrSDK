#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public class GlTexture : GlObject
    {
        static Dictionary<uint, GlTexture> _attached = [];


        protected uint _width;
        protected uint _height;
        protected bool _isCompressed;
        protected InternalFormat _internalFormat;
        protected bool _isAllocated;
        protected static uint _texReadFbId = 0;

        public GlTexture(GL gl)
            : base(gl)
        {
            WrapS = TextureWrapMode.ClampToEdge;
            WrapT = TextureWrapMode.ClampToEdge;
            MinFilter = TextureMinFilter.LinearMipmapLinear;
            MagFilter = TextureMagFilter.Linear;
            BaseLevel = 0;
            MaxLevel = 16;
            Target = TextureTarget.Texture2D;
            Create();
        }

        public GlTexture(GL gl, uint handle, uint sampleCount = 1, TextureTarget target = 0)
            : base(gl)
        {
            SampleCount = sampleCount;
            Attach(handle, target);
        }

        public GlTexture(GL gl, uint width, uint height, TextureFormat format, uint sampleCount = 1, TextureTarget target = 0)
                : base(gl)
        {
            SampleCount = sampleCount;
            Target = target;
            Update(width, height, format);
        }

        protected void Create()
        {
            _handle = _gl.GenTexture();
        }

        public unsafe void Attach(uint handle, TextureTarget target = 0)
        {
            _attached[handle] = this;

            _handle = handle;

            Target = target != 0 ? target : _gl.GetTextureTarget(handle);

            Bind();

            bool isMultiSample = Target == TextureTarget.Texture2DMultisample || Target == TextureTarget.Texture2DMultisampleArray;

            var levelTarget = Target == TextureTarget.TextureCubeMap ? TextureTarget.TextureCubeMapPositiveX : Target;

            _gl.GetTexLevelParameter(levelTarget, 0, GetTextureParameter.TextureWidth, out int w);
            _width = (uint)w;

            _gl.GetTexLevelParameter(levelTarget, 0, GetTextureParameter.TextureHeight, out int h);
            _height = (uint)h;

            //NOTE: sometimes in level 0 sometimes 1, to investigate
            for (var level = 0; level < 2; level++)
            {
                _gl.GetTexLevelParameter(levelTarget, level, GetTextureParameter.TextureInternalFormat, out int intf);
                _internalFormat = (InternalFormat)intf;
                if (intf != 0)
                    break;
            }
            //

            if (isMultiSample)
            {
                _gl.GetTexLevelParameter(levelTarget, 0, GLEnum.TextureSamples, out int sc);
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

        public unsafe IList<TextureData>? Read(TextureFormat format, uint startMipLevel = 0, uint? endMipLevel = null)
        {
            var result = new List<TextureData>();

            void ReadTarget(TextureTarget target, uint mipLevel, uint face = 0)
            {

                _gl.FramebufferTexture2D(
                     FramebufferTarget.ReadFramebuffer,
                     FramebufferAttachment.ColorAttachment0,
                     target,
                     _handle, (int)mipLevel);


                _gl.GetTexLevelParameter(target, (int)mipLevel, GetTextureParameter.TextureWidth, out int w);
                _gl.GetTexLevelParameter(target, (int)mipLevel, GetTextureParameter.TextureWidth, out int h);

                _gl.Viewport(0, 0, (uint)w, (uint)h);

                var pixelSize = format switch
                {
                    TextureFormat.Rgba32 => 32,
                    TextureFormat.Rgb24 => 24,
                    TextureFormat.SRgb24 => 24,
                    TextureFormat.RgbFloat32 => 32 * 3,
                    TextureFormat.RgbaFloat32 => 32 * 4,
                    _ => throw new NotSupportedException()
                };

                var item = new TextureData
                {
                    Width = (uint)w,
                    Height = (uint)h,
                    Format = format,
                    MipLevel = mipLevel,
                    Face = face
                };
                item.Data = new byte[pixelSize * item.Width * item.Height / 8];

                GetPixelFormat(format, out var pixelFormat, out var pixelType);

                _gl.ReadPixels(0, 0, item.Width, item.Height, pixelFormat, pixelType, item.Data.Span);

                result.Add(item);
            }

            Bind();

            if (_texReadFbId == 0)
                _texReadFbId = _gl.GenFramebuffer();

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _texReadFbId);

            if (endMipLevel == null)
                endMipLevel = MaxLevel;

            for (var mipLevel = startMipLevel; mipLevel <= endMipLevel; mipLevel++)
            {
                if (Target == TextureTarget.TextureCubeMap)
                {
                    for (var face = 0; face < 6; face++)
                        ReadTarget(TextureTarget.TextureCubeMapPositiveX + face, mipLevel, (uint)face);
                }
                else
                    ReadTarget(Target, mipLevel);
            }

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            Unbind();

            return result;
        }

        public static void GetPixelFormat(TextureFormat format, out PixelFormat pixelFormat, out PixelType pixelType)
        {
            pixelFormat = format switch
            {
                TextureFormat.Depth32Float or
                TextureFormat.Depth24Float => PixelFormat.DepthComponent,

                TextureFormat.Depth24Stencil8 => PixelFormat.DepthStencil,

                TextureFormat.SRgba32 or
                TextureFormat.RgbaFloat32 or
                TextureFormat.RgbaFloat16 or
                TextureFormat.Rgba32 => PixelFormat.Rgba,

                TextureFormat.SBgra32 or
                TextureFormat.Bgra32 => PixelFormat.Bgra,

                TextureFormat.Gray8 => PixelFormat.Red,

                TextureFormat.RgFloat32 => PixelFormat.RG,

                TextureFormat.Rgb24 or
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
                TextureFormat.Depth24Float => PixelType.Float,

                TextureFormat.RgbaFloat16 => PixelType.HalfFloat,

                TextureFormat.Depth24Stencil8 => PixelType.UnsignedInt248Oes,

                TextureFormat.Rgba32 or
                TextureFormat.Bgra32 or
                TextureFormat.Gray8 or
                TextureFormat.Rgb24 or
                TextureFormat.SRgb24 or
                TextureFormat.SBgra32 or
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
                    TextureFormat.Depth32Float => InternalFormat.DepthComponent32,
                    TextureFormat.Depth24Float => InternalFormat.DepthComponent24,
                    TextureFormat.Depth24Stencil8 => InternalFormat.Depth24Stencil8Oes,

                    TextureFormat.SBgra32 or
                    TextureFormat.SRgba32 => InternalFormat.Srgb8Alpha8,

                    TextureFormat.Rgba32 or
                    TextureFormat.Bgra32 => InternalFormat.Rgba8,

                    TextureFormat.Gray8 => InternalFormat.R8,

                    TextureFormat.RgbFloat32 => InternalFormat.Rgb32f,

                    TextureFormat.RgbaFloat32 => InternalFormat.Rgba32f,

                    TextureFormat.RgbFloat16 => InternalFormat.Rgb16f,

                    TextureFormat.RgbaFloat16 => InternalFormat.Rgba16f,

                    TextureFormat.RgFloat32 => InternalFormat.RG32f,

                    TextureFormat.Rgb24 => InternalFormat.Rgb8,

                    _ => throw new NotSupportedException(),
                };
            }

            if (compression == TextureCompressionFormat.Etc2)
            {
                return format switch
                {
                    TextureFormat.Rgb24 => InternalFormat.CompressedRgb8Etc2,
                    TextureFormat.Rgba32 => InternalFormat.CompressedRgba8Etc2Eac,
                    TextureFormat.SRgb24 => InternalFormat.CompressedSrgb8Etc2,
                    TextureFormat.SRgba32 => InternalFormat.CompressedSrgb8Alpha8Etc2Eac,
                    _ => throw new NotSupportedException(format.ToString()),
                };
            }

            if (compression == TextureCompressionFormat.Etc1)
            {
                return InternalFormat.Etc1Rgb8Oes;
            }

            throw new NotSupportedException();
        }

        public unsafe void Update(uint width, uint height, TextureFormat format, TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed, IList<TextureData>? data = null)
        {
            if (width == 0 || height == 0)
                return;

            if (EnableDebug)
                Log.Debug(this, "Update texture '{0}'", _handle);

            if (_width != width || _height != height)
                _isAllocated = false;

            _width = width;
            _height = height;

            if (data != null && data.Count > 1)
            {
                MaxLevel = data != null ? data.Max(a => a.MipLevel) : 0;

                if (MaxLevel > 0)
                {
                    if (MinFilter == TextureMinFilter.Nearest)
                        MinFilter = TextureMinFilter.NearestMipmapNearest;
                    else
                        MinFilter = TextureMinFilter.LinearMipmapLinear;
                }
                else
                {
                    if (MinFilter == TextureMinFilter.NearestMipmapNearest)
                        MinFilter = TextureMinFilter.Nearest;
                    else
                        MinFilter = TextureMinFilter.Linear;
                }
            }
            else
            {
                if (MaxLevel > 0)
                {
                    var realMax = (uint)MathF.Floor(MathF.Log2(width)) - 1;
                    if (MaxLevel > realMax)
                        MaxLevel = realMax;
                }
            }

            Update();

            Bind();

            _internalFormat = GetInternalFormat(format, compression);

            if (compression == TextureCompressionFormat.Uncompressed)
            {
                if (!_isAllocated)
                {
                    if (SampleCount > 1 && Target == TextureTarget.Texture2DMultisample)
                    {
                        _gl.TexStorage2DMultisample(
                             Target,
                             SampleCount,
                             (SizedInternalFormat)_internalFormat,
                             width,
                             height,
                             true);
                    }
                    else
                    {

                        _gl.TexStorage2D(Target,
                                  MaxLevel + 1,
                                  (SizedInternalFormat)_internalFormat,
                                  width,
                                  height);
                    }

                    _isAllocated = true;
                }


                if (data != null)
                {
                    foreach (var level in data)
                    {
                        GetPixelFormat(level.Format, out var pixelFormat, out var pixelType);

                        var realTarget = Target == TextureTarget.TextureCubeMap ?
                                             TextureTarget.TextureCubeMapPositiveX + (int)level.Face : Target;

                        _gl.TexSubImage2D(
                            realTarget,
                            (int)level.MipLevel,
                            0,
                            0,
                            level.Width,
                            level.Height,
                            pixelFormat,
                            pixelType,
                            (ReadOnlySpan<byte>)level.Data.Span);
                    }
                }
            }
            else
            {
                Debug.Assert(data != null);

                foreach (var level in data)
                {
                    var realTarget = Target == TextureTarget.TextureCubeMap ?
                                    (TextureTarget.TextureCubeMapPositiveX + (int)level.Face) :
                                    Target;

                    _gl.CompressedTexImage2D(
                        realTarget,
                        (int)level.MipLevel,
                        _internalFormat,
                        level.Width,
                        level.Height,
                        0,
                        (uint)level.Data!.Length,
                        (ReadOnlySpan<byte>)level.Data.Span);
                }

                _isCompressed = true;
            }

            if (data != null && data.Count == 1 && MaxLevel > 0 && !_isCompressed)
                _gl.GenerateMipmap(Target);
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
                _attached.Remove(_handle);
                _handle = 0;
            }

            GC.SuppressFinalize(this);
        }

        public static GlTexture Attach(GL gl, uint handle, uint sampleCount = 1)
        {
            if (!_attached.TryGetValue(handle, out var texture))
                texture = new GlTexture(gl, handle, sampleCount);
            return texture;
        }


        public long Version { get; set; }

        public TextureWrapMode WrapS { get; set; }

        public TextureWrapMode WrapT { get; set; }

        public TextureMinFilter MinFilter { get; set; }

        public TextureMagFilter MagFilter { get; set; }

        public uint SampleCount { get; set; }

        public uint BaseLevel { get; set; }

        public uint MaxLevel { get; set; }

        public TextureTarget Target { get; set; }

        public InternalFormat InternalFormat => _internalFormat;

        public bool IsCompressed => _isCompressed;

        public uint Width => _width;

        public uint Height => _height;

        public bool IsDepth => _internalFormat >= InternalFormat.DepthComponent16 && _internalFormat <= InternalFormat.DepthComponent32Sgix;
    }
}
