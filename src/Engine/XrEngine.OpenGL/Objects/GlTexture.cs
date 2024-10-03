#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlTexture : GlObject, IGlRenderAttachment
    {
        static Dictionary<uint, GlTexture> _attached = [];


        protected uint _width;
        protected uint _height;
        protected bool _isCompressed;
        protected InternalFormat _internalFormat;
        protected bool _isAllocated;
        protected static uint _texReadFbId = 0;
        protected uint _depth;

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

        protected void Create()
        {
            _handle = _gl.GenTexture();
            _attached[_handle] = this;
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

            _gl.GetTexLevelParameter(levelTarget, 0, GetTextureParameter.TextureDepthExt, out int depth);
            _depth = (uint)depth;


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

                var color = new float[4];
                _gl.GetTexParameter(Target, GetTextureParameter.TextureBorderColor, color);
                BorderColor = new Color(color);

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

                var w = Width >> (int)mipLevel;
                var h = Height >> (int)mipLevel;

                /*
                _gl.GetTexLevelParameter(target, (int)mipLevel, GetTextureParameter.TextureWidth, out int w);
                _gl.GetTexLevelParameter(target, (int)mipLevel, GetTextureParameter.TextureHeight, out int h);
                */

                GlState.Current!.SetView(new Rect2I(0, 0, (uint)w, (uint)h));

                var pixelSize = format switch
                {
                    TextureFormat.Rg88 => 16,
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

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _texReadFbId);

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

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, 0);

            Unbind();

            return result;
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

                TextureFormat.Gray8 => PixelFormat.Red,

                TextureFormat.RgFloat32 or
                TextureFormat.Rg88 => PixelFormat.RG,

                TextureFormat.RFloat32 => PixelFormat.Red,

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
                TextureFormat.RFloat32 or
                TextureFormat.Depth24Float => PixelType.Float,

                TextureFormat.RgbaFloat16 => PixelType.HalfFloat,

                TextureFormat.Depth24Stencil8 => PixelType.UnsignedInt248Oes,

                TextureFormat.Depth32Stencil8 => PixelType.Float32UnsignedInt248Rev,

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

        public void Update(uint arraySize, params TextureData[] data)
        {
            Update(data[0].Width, data[0].Height, arraySize, data[0].Format, data[0].Compression, data); 
        }

        public unsafe void Update(uint width, uint height, uint depth, TextureFormat format, TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed, IList<TextureData>? data = null)
        {
            if (width == 0 || height == 0)
                return;

            if (EnableDebug)
                Log.Debug(this, "Update texture '{0}'", _handle);

            if (_width != width || _height != height || _depth != depth)
                _isAllocated = false;

            _width = width;
            _height = height;
            _depth = depth;

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

            Bind();

            UpdateWork();

            _internalFormat = GetInternalFormat(format, compression);

            if (compression == TextureCompressionFormat.Uncompressed)
            {
                if (!_isAllocated && !IsMutable)
                {
                    if (_depth > 1)
                    {
                        if (SampleCount > 1 && Target == TextureTarget.Texture2DMultisampleArray)
                            throw new NotSupportedException();
                        else
                        {
                            _gl.TexStorage3D(
                                Target,
                                MaxLevel + 1,
                                (SizedInternalFormat)_internalFormat,
                                width,
                                height,
                                _depth);
                        }
                    }
                    else
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

                        if (level.Data.Length == 0 && !_isAllocated)
                        {
                            if (data.Count == 1 && data[0].MipLevel == 0)
                            {
                                _gl.TexImage2D(
                                realTarget,
                                0,
                                _internalFormat,
                                level.Width,
                                level.Height,
                                0,
                                pixelFormat,
                                pixelType,
                                null);

                            }
                            else
                            {
                                _gl.TexSubImage2D(
                                   realTarget,
                                   (int)level.MipLevel,
                                   0,
                                   0,
                                   level.Width,
                                   level.Height,
                                   pixelFormat,
                                   pixelType,
                                   null);
                            }
                   
                        }
                        else if (level.Data.Length > 0)
                        {
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

            UpdateWork();

            Unbind();
        }

        protected void UpdateWork()
        {
            bool isMultiSample = Target == TextureTarget.Texture2DMultisample || Target == TextureTarget.Texture2DMultisampleArray;

            if (!isMultiSample)
            {
                _gl.TexParameter(Target, TextureParameterName.TextureWrapS, (int)WrapS);
                _gl.TexParameter(Target, TextureParameterName.TextureWrapT, (int)WrapT);
                _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)MinFilter);
                _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)MagFilter);
                _gl.TexParameter(Target, TextureParameterName.TextureBorderColor, BorderColor.ToArray());
            }

            if (!IsDepth)
            {
                _gl.TexParameter(Target, TextureParameterName.TextureBaseLevel, BaseLevel);
                _gl.TexParameter(Target, TextureParameterName.TextureMaxLevel, MaxLevel);
            }
        }

        public void Bind()
        {
            GlState.Current!.SetActiveTexture(this, Slot);   
        }

        public void Unbind()
        {
            GlState.Current!.BindTexture(Target, 0);
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

        public static GlTexture Attach(GL gl, uint handle, uint sampleCount = 1, TextureTarget target = 0)
        {
            if (!_attached.TryGetValue(handle, out var texture))
                texture = new GlTexture(gl, handle, sampleCount, target);
            return texture;
        }


        public long Version { get; set; }

        public TextureWrapMode WrapS { get; set; }

        public TextureWrapMode WrapT { get; set; }

        public TextureMinFilter MinFilter { get; set; }

        public TextureMagFilter MagFilter { get; set; }

        public Color BorderColor { get; set; }

        public uint SampleCount { get; set; }

        public uint BaseLevel { get; set; }

        public uint MaxLevel { get; set; }

        public int Slot { get; set; }

        public bool IsMutable { get; set; }

        public TextureTarget Target { get; set; }

        public InternalFormat InternalFormat => _internalFormat;

        public bool IsCompressed => _isCompressed;

        public uint Width => _width;

        public uint Height => _height;

        public uint Depth => _depth;

        public bool IsDepth => _internalFormat >= InternalFormat.DepthComponent16 && _internalFormat <= InternalFormat.DepthComponent32Sgix;

    }
}
