#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
#else
using Silk.NET.OpenGL;
#endif

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

        protected uint _depth;
        protected bool _isAttached;


        public GlTexture(GL gl)
            : base(gl)
        {
            WrapS = TextureWrapMode.ClampToEdge;
            WrapT = TextureWrapMode.ClampToEdge;
            WrapR = TextureWrapMode.ClampToEdge;
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

        public void SetTarget(TextureTarget target)
        {
            if (target == 0 || Target == target)
                return;

            if (_isAttached)
                throw new InvalidOperationException("Cannot change the target of an attached texture");

            if (_isAllocated)
                Recreate();

            Target = target;
        }

        public void Attach(uint handle, TextureTarget target = 0)
        {
            if (_handle == handle)
                return;

            if (_handle != 0)
            {
                Log.Warn(this, "Attached an existing texture {0}- {1}", _handle, handle);
                Destroy();
            }
     
            _attached[handle] = this;

            _handle = handle;
            _isAttached = true;
            _isAllocated = true;

            Target = target != 0 ? target : _gl.GetTextureTarget(handle);

            Bind();

            var isMultiSample =
                Target == TextureTarget.Texture2DMultisample ||
                Target == TextureTarget.Texture2DMultisampleArray;

            var levelTarget = Target == TextureTarget.TextureCubeMap
                ? TextureTarget.TextureCubeMapPositiveX
                : Target;

            _gl.GetTexLevelParameter(levelTarget, 0, GetTextureParameter.TextureWidth, out int w);
            _width = (uint)w;

            _gl.GetTexLevelParameter(levelTarget, 0, GetTextureParameter.TextureHeight, out int h);
            _height = (uint)h;

            _gl.GetTexLevelParameter(levelTarget, 0, GetTextureParameter.TextureDepthExt, out int depth);
            _depth = Math.Max((uint)depth, 1);

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

                _gl.GetTexParameter(Target, GetTextureParameter.TextureWrapRExt, out int wr);
                WrapR = (TextureWrapMode)wr;

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

            for (var level = (int)BaseLevel; level <= 2; level++)
            {
                _gl.GetTexLevelParameter(levelTarget, level, GetTextureParameter.TextureInternalFormat, out int intf);
                _internalFormat = (InternalFormat)intf;

                if (intf != 0)
                    break;

                if (level == BaseLevel)
                    Log.Warn(this, "Tex {0} Internal format not found at level {1}", _handle, level);
            }

            if (_internalFormat.HasDepth() &&
                (MinFilter != TextureMinFilter.Nearest || MagFilter != TextureMagFilter.Nearest))
            {
                MinFilter = TextureMinFilter.Nearest;
                MagFilter = TextureMagFilter.Nearest;

                _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)MinFilter);
                _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)MagFilter);

                this.DumpState();
            }

            Unbind();
        }

        public void CopyTo(GlTexture dest, int level = 0, int depth = 0)
        {
            _gl.CopyImageSubData(
                _handle,
                (CopyImageSubDataTarget)Target,
                level,
                0,
                0,
                depth,
                dest.Handle,
                (CopyImageSubDataTarget)dest.Target,
                level,
                0,
                0,
                depth,
                _width,
                _height,
                Math.Max(_depth, 1));
        }

        public void Update(params TextureData[] data)
        {
            if (data.Length == 0)
                throw new InvalidOperationException("Texture data is empty");

            var width = data.Max(a => a.Width);
            var height = data.Max(a => a.Height);
            var depth = GetDataDepth(data);

            UploadFull(
                width,
                height,
                depth,
                data[0].Format,
                data[0].Compression,
                data,
                data[0].BlockSize);
        }


        public void Allocate(
            uint width,
            uint height,
            uint depth,
            TextureFormat format,
            TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed,
            uint blockSize = 0)
        {
            if (width == 0 || height == 0)
                return;

            if (compression != TextureCompressionFormat.Uncompressed)
                throw new InvalidOperationException("Compressed textures require a full compressed upload");

            if (EnableDebug)
                GlDebug.Log(this, "Allocate texture '{0}' {1}x{2}x{3}", _handle, width, height, Math.Max(depth, 1));

            _internalFormat = GlUtils.GetInternalFormat(format, compression, blockSize);

            if (depth > 1 && Target == TextureTarget.Texture2D)
                Target = TextureTarget.Texture2DArray;

            ClampMaxLevel(width, height);

            Bind();

            UpdateSampler();

            AllocateStorage(width, height, depth, format);

            Unbind();
        }

        public void UploadFull(IList<TextureData> data)
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Texture data is empty");

            var width = data.Max(a => a.Width);
            var height = data.Max(a => a.Height);
            var depth = GetDataDepth(data);
            var blockSize = data[0].BlockSize;

            UploadFull(width, height, depth, data[0].Format, data[0].Compression, data, blockSize);
        }

        public void UploadFull(
            uint width,
            uint height,
            uint depth,
            TextureFormat format,
            TextureCompressionFormat compression,
            IList<TextureData> data,
            uint blockSize = 0)
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Texture data is empty");

            if (width == 0 || height == 0)
                return;

            if (EnableDebug)
                GlDebug.Log(this, "Upload texture '{0}' {1}x{2}x{3}", _handle, width, height, Math.Max(depth, 1));

            if (data.Count > 1)
                MaxLevel = data.Max(a => a.MipLevel);
            else
                ClampMaxLevel(width, height);

            _internalFormat = GlUtils.GetInternalFormat(format, compression, blockSize);

            Bind();

            UpdateSampler();

            if (compression == TextureCompressionFormat.Uncompressed)
                UploadUncompressedFull(width, height, depth, data[0].Format, data);
            else
                UploadCompressedFull(width, height, depth, data);

            if (data.Count == 1 && MaxLevel > 0 && !_isCompressed)
                _gl.GenerateMipmap(Target);

            Unbind();
        }

        public unsafe void UploadRegion(TextureRegion region)
        {
            if (region.Data == null)
                throw new InvalidOperationException("Upload region has no data");

            if (!_isAllocated)
                throw new InvalidOperationException("Texture storage is not allocated");

            if (_isCompressed)
                throw new NotSupportedException("Use full compressed uploads for compressed textures");

            Bind();

            GlUtils.GetPixelFormat(region.Format, out var pixelFormat, out var pixelType);

            using var pData = region.Data.MemoryLock();

            var realTarget = GetLayerTarget(region.Layer);
            var uploadDepth = Math.Max(region.Depth, 1);

            if (Target != TextureTarget.TextureCubeMap && _depth > 1)
            {
                _gl.TexSubImage3D(
                    realTarget,
                    (int)region.MipLevel,
                    region.X,
                    region.Y,
                    region.Z,
                    region.Width,
                    region.Height,
                    uploadDepth,
                    pixelFormat,
                    pixelType,
                    pData);
            }
            else
            {
                _gl.TexSubImage2D(
                    realTarget,
                    (int)region.MipLevel,
                    region.X,
                    region.Y,
                    region.Width,
                    region.Height,
                    pixelFormat,
                    pixelType,
                    pData);
            }

            Unbind();
        }

        public void Clear(Color color, int level = 0)
        {
            var colorSpan = color.ToArray();

#if GLES
            if (_clearExt == null)
                _gl.TryGetExtension<ExtClearTexture>(out _clearExt);

            _clearExt!.ClearTexImage(_handle, level, PixelFormat.Rgba, PixelType.Float, colorSpan.AsSpan());
#else
            _gl.ClearTexImage(_handle, level, PixelFormat.Rgba, PixelType.Float, colorSpan.AsSpan());
#endif
        }

        public void OverrideSize(uint width, uint height)
        {
            _width = width;
            _height = height;
        }

        public void Update()
        {
            Bind();
            UpdateSampler();
            Unbind();
        }

        protected internal void UpdateSampler()
        {
            var isMultiSample =
                Target == TextureTarget.Texture2DMultisample ||
                Target == TextureTarget.Texture2DMultisampleArray;

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
                else if (MinFilter == TextureMinFilter.LinearMipmapLinear)
                    MinFilter = TextureMinFilter.Linear;
            }

            if (!isMultiSample)
            {
                _gl.TexParameter(Target, TextureParameterName.TextureWrapS, (int)WrapS);
                _gl.TexParameter(Target, TextureParameterName.TextureWrapT, (int)WrapT);
                _gl.TexParameter(Target, TextureParameterName.TextureWrapR, (int)WrapR);
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

                if (!_isAttached)
                    _gl.DeleteTexture(_handle);

                _attached.Remove(_handle);

                if (EnableDebug && !_isAttached)
                    GlDebug.Log(this, "Tex {0} deleted", _handle);
            }

            if (Source is Texture tex)
            {
                tex.DeleteProp(OpenGLRender.Props.GlResId);
                tex.Handle = 0;
            }

            _isAllocated = false;
            _isAttached = false;
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


        protected unsafe void UploadUncompressedFull(
            uint width,
            uint height,
            uint depth,
            TextureFormat format,
            IList<TextureData> data)
        {
            AllocateStorage(width, height, depth, format);

            GlUtils.GetPixelFormat(format, out var pixelFormat, out var pixelType);

            foreach (var entry in data)
            {
                if (entry.Data == null)
                    continue;

                if (entry.Data.Size == 0)
                    throw new InvalidOperationException();

                var realTarget = GetLayerTarget(entry.Layer);
                var uploadDepth = Math.Max(entry.Depth, 1);

                using var pData = entry.Data.MemoryLock();

                if (Target != TextureTarget.TextureCubeMap && _depth > 1)
                {
                    _gl.TexSubImage3D(
                        realTarget,
                        (int)entry.MipLevel,
                        0,
                        0,
                        (int)entry.Layer,
                        entry.Width,
                        entry.Height,
                        uploadDepth,
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

            _isCompressed = false;
        }

        protected unsafe void UploadCompressedFull(
            uint width,
            uint height,
            uint depth,
            IList<TextureData> data)
        {
            if (Target == TextureTarget.Texture2DArray ||
                Target == TextureTarget.Texture3D ||
                Target == TextureTarget.Texture2DMultisampleArray)
            {
                AllocateCompressedArrayStorage(width, height, depth);

                foreach (var entry in data)
                {
                    if (entry.Data == null)
                        throw new InvalidOperationException("Compressed texture data is missing");

                    using var pData = entry.Data.MemoryLock();

                    _gl.CompressedTexSubImage3D(
                        Target,
                        (int)entry.MipLevel,
                        0,
                        0,
                        (int)entry.Layer,
                        entry.Width,
                        entry.Height,
                        entry.Depth,
                        _internalFormat,
                        entry.Data.Size,
                        pData);

                    _gl.CheckError();
                }
            }
            else
            {
                foreach (var entry in data)
                {
                    if (entry.Data == null)
                        throw new InvalidOperationException("Compressed texture data is missing");

                    var realTarget = GetLayerTarget(entry.Layer);

                    using var pData = entry.Data.MemoryLock();

                    _gl.CompressedTexImage2D(
                        realTarget,
                        (int)entry.MipLevel,
                        _internalFormat,
                        entry.Width,
                        entry.Height,
                        0,
                        entry.Data.Size,
                        pData);

                    _gl.CheckError();
                }

                _width = width;
                _height = height;
                _depth = Math.Max(depth, 1);
                _isAllocated = true;
            }

            _isCompressed = true;
        }

        protected void AllocateStorage(
            uint width,
            uint height,
            uint depth,
            TextureFormat format)
        {
            var normalizedDepth = Math.Max(depth, 1);

            if (_width != width ||
                _height != height ||
                _depth != normalizedDepth ||
                _internalFormat != 0 && _internalFormat != GlUtils.GetInternalFormat(format, TextureCompressionFormat.Uncompressed, 0))
            {
                if (!IsMutable && _isAllocated)
                    throw new InvalidOperationException("Immutable texture storage changed");

                _isAllocated = false;
            }

            _width = width;
            _height = height;
            _depth = normalizedDepth;

            if (_isAllocated)
                return;

            if (SampleCount > 1)
            {
                AllocateMultisampleStorage(width, height, normalizedDepth);
                _isAllocated = true;
                return;
            }

            if (!IsMutable)
            {
                AllocateImmutableStorage(width, height, normalizedDepth);
                _isAllocated = true;
                return;
            }

            AllocateMutableStorage(width, height, normalizedDepth, format);
            _isAllocated = true;
        }

        protected void AllocateImmutableStorage(uint width, uint height, uint depth)
        {
            if (depth > 1 && Target != TextureTarget.TextureCubeMap)
            {
                _gl.TexStorage3D(
                    Target,
                    MaxLevel + 1,
                    (SizedInternalFormat)_internalFormat,
                    width,
                    height,
                    depth);
            }
            else
            {
                _gl.TexStorage2D(
                    Target,
                    MaxLevel + 1,
                    (SizedInternalFormat)_internalFormat,
                    width,
                    height);
            }
        }

        protected unsafe void AllocateMutableStorage(
            uint width,
            uint height,
            uint depth,
            TextureFormat format)
        {
            GlUtils.GetPixelFormat(format, out var pixelFormat, out var pixelType);

            if (Target == TextureTarget.TextureCubeMap)
            {
                for (var face = 0; face < 6; face++)
                {
                    var realTarget = (TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + face);

                    for (uint level = 0; level <= MaxLevel; level++)
                    {
                        var levelWidth = GetMipSize(width, level);
                        var levelHeight = GetMipSize(height, level);

                        _gl.TexImage2D(
                            realTarget,
                            (int)level,
                            _internalFormat,
                            levelWidth,
                            levelHeight,
                            0,
                            pixelFormat,
                            pixelType,
                            null);
                    }
                }

                return;
            }

            if (depth > 1)
            {
                for (uint level = 0; level <= MaxLevel; level++)
                {
                    var levelWidth = GetMipSize(width, level);
                    var levelHeight = GetMipSize(height, level);

                    _gl.TexImage3D(
                        Target,
                        (int)level,
                        _internalFormat,
                        levelWidth,
                        levelHeight,
                        depth,
                        0,
                        pixelFormat,
                        pixelType,
                        null);
                }

                return;
            }

            for (uint level = 0; level <= MaxLevel; level++)
            {
                var levelWidth = GetMipSize(width, level);
                var levelHeight = GetMipSize(height, level);

                _gl.TexImage2D(
                    Target,
                    (int)level,
                    _internalFormat,
                    levelWidth,
                    levelHeight,
                    0,
                    pixelFormat,
                    pixelType,
                    null);
            }
        }

        protected void AllocateMultisampleStorage(uint width, uint height, uint depth)
        {
            if (depth > 1 && Target == TextureTarget.Texture2DMultisampleArray)
            {
                _gl.TexStorage3DMultisample(
                    Target,
                    SampleCount,
                    (SizedInternalFormat)_internalFormat,
                    width,
                    height,
                    depth,
                    true);

                return;
            }

            _gl.TexStorage2DMultisample(
                Target,
                SampleCount,
                (SizedInternalFormat)_internalFormat,
                width,
                height,
                true);
        }

        protected void AllocateCompressedArrayStorage(uint width, uint height, uint depth)
        {
            var normalizedDepth = Math.Max(depth, 1);

            if (_width != width ||
                _height != height ||
                _depth != normalizedDepth ||
                _internalFormat == 0)
            {
                if (_isAllocated)
                    throw new InvalidOperationException("Compressed array texture storage changed");

                _isAllocated = false;
            }

            _width = width;
            _height = height;
            _depth = normalizedDepth;

            if (_isAllocated)
                return;

            _gl.TexStorage3D(
                Target,
                MaxLevel + 1,
                (SizedInternalFormat)_internalFormat,
                width,
                height,
                normalizedDepth);

            _isAllocated = true;
        }

        protected TextureTarget GetLayerTarget(uint layer)
        {
            if (Target == TextureTarget.TextureCubeMap)
                return (TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + (int)layer);

            return Target;
        }

        protected void ClampMaxLevel(uint width, uint height)
        {
            if (MaxLevel == 0)
                return;

            var realMax = (uint)MathF.Floor(MathF.Log2(Math.Max(width, height)));

            if (MaxLevel > realMax)
                MaxLevel = realMax;
        }

        protected static uint GetMipSize(uint size, uint level)
        {
            return Math.Max(size >> (int)level, 1);
        }

        protected static uint GetDataDepth(IList<TextureData> data)
        {
            var depthFromLayers = data.Max(a => a.Layer) + 1;
            var depthFromData = data.Max(a => a.Depth);

            return Math.Max(depthFromLayers, Math.Max(depthFromData, 1));
        }


        public long Version { get; set; }

        public TextureWrapMode WrapS { get; set; }

        public TextureWrapMode WrapT { get; set; }

        public TextureWrapMode WrapR { get; set; }

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

        public bool IsAttached => _isAttached;

        public bool IsDepth => _internalFormat.IsDepth();
    }
}