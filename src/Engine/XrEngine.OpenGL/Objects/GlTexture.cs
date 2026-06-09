#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
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

#if GLES
        static ExtClearTexture? _clearExt;
#endif


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

        public void Recreate()
        {
            Destroy();
            Create();
        }

        protected void Create()
        {
            _handle = _gl.GenTexture();
            _attached[_handle] = this;
        }

        public void Attach(uint handle, TextureTarget target = 0)
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

        public void CopyTo(GlTexture dest, int level = 0, int depth = 0)
        {
            _gl.CopyImageSubData(_handle, (CopyImageSubDataTarget)Target, level, 0, 0, depth, dest.Handle, (CopyImageSubDataTarget)dest.Target, level, 0, 0, depth, _width, _height, _depth);
        }

        public unsafe IList<TextureData>? Read(TextureFormat format, uint startMipLevel = 0, uint? endMipLevel = null, IList<IMemoryBuffer<byte>>? buffers = null)
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

                var pixelSize = format.GetPixelSizeBit();

                var bufferSize = (pixelSize / 8) * w * h ;

                var buffer = buffers?[result.Count] ?? MemoryBuffer.Create<byte>((uint)bufferSize);
                buffer.Allocate((uint)bufferSize);

                var item = new TextureData
                {
                    Width = w,
                    Height = h,
                    Format = format,
                    MipLevel = mipLevel,
                    Layer = face,
                    Data = buffer
                };

                GlUtils.GetPixelFormat(format, out var pixelFormat, out var pixelType);

                using var pData = buffer.MemoryLock();

                GlState.Current.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);

                _gl.ReadPixels(0, 0, item.Width, item.Height, pixelFormat, pixelType, pData);

                _gl.CheckError();

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

        public void Update(params TextureData[] data)
        {
            if (data.Length == 0)
                throw new InvalidOperationException();

            Update(data[0].Width, data[0].Height, data[0].Depth, data[0].Format, data[0].Compression, data, data[0].BlockSize);
        }

        //TODO: separate allocation from update
        public unsafe void Update(uint width, uint height, uint depth, TextureFormat format, TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed, IList<TextureData>? data = null, uint blockSize = 0)
        {
            if (width == 0 || height == 0)
                return;

            if (EnableDebug)
                Log.Debug(this, "Update texture '{0}' {1}x{2}", _handle, width, height);

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
                MaxLevel = data.Max(a => a.MipLevel);
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

            _internalFormat = GlUtils.GetInternalFormat(format, compression, blockSize);

            if (compression == TextureCompressionFormat.Uncompressed)
            {
                Debug.Assert(!_isCompressed);

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
                                depth);
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
                    foreach (var entry in data)
                    {
                        var realTarget = Target == TextureTarget.TextureCubeMap ?
                                                   TextureTarget.TextureCubeMapPositiveX + (int)entry.Layer : Target;

                        byte* pData = null;

                        if (entry.Data != null)
                            pData = entry.Data.Lock();

                        if (!_isAllocated || pData != null)
                        {
                            GlUtils.GetPixelFormat(entry.Format, out var pixelFormat, out var pixelType);

                            if (!_isAllocated)
                            {
                                Debug.Assert(IsMutable);

                                if (_depth > 1)
                                {
#warning I SHOULD ALLOCATE ONLY NEEDED MIP LEVELS AND NOT ONE TexImage3D for each LAYER 
                                    _gl.TexImage3D(
                                         realTarget,
                                         (int)entry.MipLevel,
                                         _internalFormat,
                                         entry.Width,
                                         entry.Height,
                                         entry.Depth,
                                         0,
                                         pixelFormat,
                                         pixelType,
                                         pData);
                                }
                                else
                                {
                                    _gl.TexImage2D(
                                          realTarget,
                                          (int)entry.MipLevel,
                                          _internalFormat,
                                          entry.Width,
                                          entry.Height,
                                          0,
                                          pixelFormat,
                                          pixelType,
                                          pData);
                                }

                            }
                            else if (_isAllocated)
                            {
                                if (_depth > 1)
                                {
                                    _gl.TexSubImage3D(
                                         realTarget,
                                         (int)entry.MipLevel,
                                         0,
                                         0,
                                         (int)entry.Layer,
                                         entry.Width,
                                         entry.Height,
                                         entry.Depth,
                                         pixelFormat,
                                         pixelType,
                                         pData);
                                }
                                else
                                {
                                    _gl.TexSubImage2D(
                                            realTarget,
                                            (int)entry.MipLevel,
                                            0,
                                            0,
                                            entry.Width,
                                            entry.Height,
                                            pixelFormat,
                                            pixelType,
                                            pData);
                                }

                            }

                        }

                        entry.Data?.Unlock();
                    }


                    _isAllocated = true;
                }
            }
            else
            {
                Debug.Assert(data != null);

                uint maxLevel = 0;

                foreach (var level in data)
                {
                    var realTarget = Target == TextureTarget.TextureCubeMap ?
                                    (TextureTarget.TextureCubeMapPositiveX + (int)level.Layer) :
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

                    _gl.CheckError();

                    maxLevel = Math.Max(level.MipLevel, maxLevel);
                }

                if (maxLevel != MaxLevel)
                {
                    MaxLevel = maxLevel;
                    UpdateSampler();
                }

                _isCompressed = true;
            }

            if (data != null && data.Count == 1 && MaxLevel > 0 && !_isCompressed)
                _gl.GenerateMipmap(Target);
        }

        public void Clear(Color color, int level = 0)
        {
            var colorSpan = color.ToArray();

#if GLES
            if (_clearExt == null)
                _gl.TryGetExtension<ExtClearTexture>(out _clearExt);

            _clearExt.ClearTexImage(_handle, level, PixelFormat.Rgba, PixelType.Float, colorSpan.AsSpan());
#else
            _gl.ClearTexImage(_handle, level, PixelFormat.Rgba, PixelType.Float, colorSpan.AsSpan());
#endif
        }

        public void Update()
        {
            Bind();

            UpdateSampler();

            Unbind();
        }

        protected internal void UpdateSampler()
        {
            var isMultiSample = Target == TextureTarget.Texture2DMultisample || Target == TextureTarget.Texture2DMultisampleArray;

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

        public void GenerateMipmap()
        {
            GlState.Current!.BindTexture(Target, _handle);
            _gl.GenerateMipmap(Target);
        }

        public void Bind(bool force = false)
        {
            GlState.Current!.SetActiveTexture(Slot, force);
            GlState.Current!.LoadTexture(this, Slot, force);
        }

        public void Unbind()
        {
            GlState.Current!.BindTexture(Target, 0);
        }

        protected void Destroy()
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

            _isAllocated = false;
            _width = 0;
            _height = 0;
            _isCompressed = false;
            _depth = 0;
            _internalFormat = 0;
        }

        public override void Dispose()
        {
            Destroy();

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
