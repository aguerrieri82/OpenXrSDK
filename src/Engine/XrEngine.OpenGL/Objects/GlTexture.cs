#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
using System.Diagnostics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlTexture : GlObject, IGlRenderAttachment
    {
        static internal readonly Dictionary<uint, GlTexture> _attached = [];


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

            var isMultiSample = Target == TextureTarget.Texture2DMultisample || Target == TextureTarget.Texture2DMultisampleArray;

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

                _gl.GetTexParameter(Target, (GLEnum)TextureParameterName.TextureMaxAnisotropy, out float asin);
                MaxAnisotropy = asin;

                var color = new float[4];
                _gl.GetTexParameter(Target, GetTextureParameter.TextureBorderColor, color);
                BorderColor = new Color(color);

            }

            _gl.GetTexParameter(Target, GetTextureParameter.TextureBaseLevelSgis, out int bl);
            BaseLevel = (uint)bl;

            _gl.GetTexParameter(Target, GetTextureParameter.TextureMaxLevelSgis, out int ml);
            MaxLevel = (uint)ml;

#warning IMPROVE
            if (GlUtils.IsDepth(InternalFormat) && (MinFilter != TextureMinFilter.Nearest || MagFilter != TextureMagFilter.Nearest))
            {
                MinFilter = TextureMinFilter.Nearest;
                MagFilter = TextureMagFilter.Nearest;
                _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)MinFilter);
                _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)MagFilter);
            }

            Unbind();
        }

        public unsafe IList<TextureData>? Read(TextureFormat format, uint startMipLevel = 0, uint? endMipLevel = null)
        {
            var result = new List<TextureData>();

            void ReadTarget(TextureTarget target, uint mipLevel, uint face = 0, uint depth = 0)
            {
                if (target == TextureTarget.Texture2DArray)
                {
                    _gl.FramebufferTextureLayer(
                         FramebufferTarget.ReadFramebuffer,
                         FramebufferAttachment.ColorAttachment0,
                         _handle,
                         (int)mipLevel,
                         (int)depth);
                }
                else
                {
                    _gl.FramebufferTexture2D(
                         FramebufferTarget.ReadFramebuffer,
                         FramebufferAttachment.ColorAttachment0,
                         target,
                         _handle, (int)mipLevel);
                }

                var status = _gl.CheckFramebufferStatus(FramebufferTarget.ReadFramebuffer);
                if (status != GLEnum.FramebufferComplete)
                    throw new Exception($"Framebuffer incomplete at mip {mipLevel}: {status}");

                var w = Width >> (int)mipLevel;
                var h = Height >> (int)mipLevel;

                GlState.Current!.SetView(new Rect2I(0, 0, w, h));

                var pixelSize = GlUtils.GetPixelSizeBit(format);

                var item = new TextureData
                {
                    Width = w,
                    Height = h,
                    Format = format,
                    MipLevel = mipLevel,
                    Face = face,
                    Data = MemoryBuffer.Create<byte>(pixelSize * w * h / 8)
                };

                GlUtils.GetPixelFormat(format, out var pixelFormat, out var pixelType);

                using var pData = item.Data.MemoryLock();

                GlState.Current.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);

                _gl.ReadPixels(0, 0, item.Width, item.Height, pixelFormat, pixelType, pData);

                result.Add(item);
            }

            Bind();

            if (_texReadFbId == 0)
                _texReadFbId = _gl.GenFramebuffer();

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _texReadFbId);
            _gl.ReadBuffer(GLEnum.ColorAttachment0);

            if (endMipLevel == null)
                endMipLevel = MaxLevel;

            for (var mipLevel = startMipLevel; mipLevel <= endMipLevel; mipLevel++)
            {
                if (Target == TextureTarget.TextureCubeMap)
                {
                    for (var face = 0; face < 6; face++)
                        ReadTarget(TextureTarget.TextureCubeMapPositiveX + face, mipLevel, (uint)face);
                }
                else if (Target == TextureTarget.Texture2DArray)
                {
                    for (uint i = 0; i < _depth; i++)
                        ReadTarget(Target, mipLevel, 0, i);
                }
                else
                    ReadTarget(Target, mipLevel);
            }

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, 0);

            Unbind();

            return result;
        }



        public void Update(uint depth, params TextureData[] data)
        {
            if (data.Length == 0)
                throw new InvalidOperationException();

            Update(data[0].Width, data[0].Height, depth, data[0].Format, data[0].Compression, data);
        }

        public unsafe void Update(uint width, uint height, uint depth, TextureFormat format, TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed, IList<TextureData>? data = null)
        {
            if (width == 0 || height == 0)
                return;

            if (EnableDebug)
                Log.Debug(this, "Update texture '{0}'", _handle);

            if (_width != width || _height != height || _depth != depth)
            {
                if (!IsMutable && _isAllocated)
                    throw new InvalidOperationException("Immutable texture size changed");
                _isAllocated = false;
            }


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
                    var realMax = (uint)MathF.Floor(MathF.Log2(Math.Max(_width, _height)));
                    if (MaxLevel > realMax)
                        MaxLevel = realMax;
                }
            }

            Bind();

            UpdateSampler();

            _internalFormat = GlUtils.GetInternalFormat(format, compression);

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
                    bool hasOneLevel = data.Count == 1 && data[0].MipLevel == 0;

                    foreach (var level in data)
                    {
         
                        var realTarget = Target == TextureTarget.TextureCubeMap ?
                                             TextureTarget.TextureCubeMapPositiveX + (int)level.Face : Target;

                        byte* pData = null;

                        if (level.Data != null)
                            pData = level.Data.Lock();

                        if (!_isAllocated || pData != null)
                        {
                            GlUtils.GetPixelFormat(level.Format, out var pixelFormat, out var pixelType);

                            if (hasOneLevel && IsMutable)
                            {
                                if (_depth > 1)
                                {
                                    _gl.TexImage3D(
                                         realTarget,
                                         0,
                                         _internalFormat,
                                         level.Width,
                                         level.Height,
                                         _depth,
                                         0,
                                         pixelFormat,
                                         pixelType,
                                         pData);
                                }
                                else
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
                                          pData);
                                }
                            }
                            else
                            {
                                if (_depth > 1)
                                {
                                    _gl.TexSubImage3D(
                                         realTarget,
                                         (int)level.MipLevel,
                                         0,
                                         0,
                                         0,
                                         level.Width,
                                         level.Height,
                                         level.Depth,
                                         pixelFormat,
                                         pixelType,
                                         pData);
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
                                            pData);
                                }

                            }

                            _isAllocated = true;
                        }

                        level.Data?.Unlock();
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

                    Debug.Assert(level.Data != null);

                    using var pData = level.Data.MemoryLock();

                    _gl.CompressedTexImage2D(
                        realTarget,
                        (int)level.MipLevel,
                        _internalFormat,
                        level.Width,
                        level.Height,
                        0,
                        level.Data.Size,
                        pData);

                }

                _isCompressed = true;
            }

            if (data != null && data.Count == 1 && MaxLevel > 0 && !_isCompressed)
                _gl.GenerateMipmap(Target);
        }

        public void UpdateDate(params TextureData[] data)
        {

        }

        public void Update()
        {
            Bind();

            UpdateSampler();

            Unbind();
        }

        protected internal void UpdateSampler()
        {
            bool isMultiSample = Target == TextureTarget.Texture2DMultisample || Target == TextureTarget.Texture2DMultisampleArray;

            if (!isMultiSample)
            {
                _gl.TexParameter(Target, TextureParameterName.TextureWrapS, (int)WrapS);
                _gl.TexParameter(Target, TextureParameterName.TextureWrapT, (int)WrapT);
                _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)MinFilter);
                _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)MagFilter);
                _gl.TexParameter(Target, TextureParameterName.TextureBorderColor, BorderColor.ToArray());
                if (MaxAnisotropy > 0)
                    _gl.TexParameter(Target, TextureParameterName.TextureMaxAnisotropy, MaxAnisotropy);
            }

            if (!IsDepth)
            {
                _gl.TexParameter(Target, TextureParameterName.TextureBaseLevel, BaseLevel);
                _gl.TexParameter(Target, TextureParameterName.TextureMaxLevel, MaxLevel);
            }
        }



        public void Bind()
        {
            GlState.Current!.LoadTexture(this, Slot);
        }

        public void Unbind()
        {
            GlState.Current!.BindTexture(Target, 0);
        }

        public override void Dispose()
        {
            if (_handle != 0)
            {
                GlState.Current!.ResetTextures();
                _gl.DeleteTexture(_handle);
                _attached.Remove(_handle);
            }

            if (Source is Texture tex)
            {
                tex.DeleteProp(OpenGLRender.Props.GlResId);
                tex.Handle = 0;
            }

            Source = null;

            base.Dispose();
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

        public float MaxAnisotropy { get; set; }

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
