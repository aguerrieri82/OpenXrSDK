#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;


#endif

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlBlurMipOptions
    {
        public GlBlurMipOptions()
        {
            MaxLevels = 6;
            MinLevelSize = 64;
            MaxBlurRadius = 2;
            AllocationBlock = 64;
            MaxTerminalPasses = 4;
            MinTerminalRadius = 1.25f;
        }

        public int MaxLevels;
        public uint MinLevelSize;
        public float MaxBlurRadius;
        public uint AllocationBlock;
        public int MaxTerminalPasses;
        public float MinTerminalRadius;
    }


    [StructLayout(LayoutKind.Explicit, Size = 112)]
    public struct GlBlurMipLayout : ITextureLayout
    {
        [InlineArray(6)]
        public struct Vector4Array6
        {
            private Vector4 _element;
        }

        public GlBlurMipLayout(Vector4[] levels, float levelScale)
        {
            if (levels.Length > 6)
                throw new ArgumentOutOfRangeException(nameof(levels));

            this = default;
            LevelScale = levelScale;

            for (var i = 0; i < levels.Length; i++)
                Levels[i] = levels[i];
        }

        public readonly void Update(UpdateShaderContext ctx, IUniformProvider up, Texture source, uint slot = 0)
        {
            var buffer = ctx.BufferProvider!.GetBuffer<GlBlurMipLayout>(UniformsSlots.BlurMip, BufferStore.Model, BufferUsage.Uniforms);

            buffer.Update(this);

            up.LoadBuffer(buffer, UniformsSlots.BlurMip, BufferUsage.Uniforms);
        }

        [FieldOffset(0)]
        public Vector4Array6 Levels;

        [FieldOffset(96)]
        public float LevelScale;
    }

    public class GlBlurMipPack : IDisposable
    {
        private const uint GroupSize = 8;
        private const uint Padding = 1;
        private const int MaxTaps = 12;

        private struct Region
        {
            public uint Width;
            public uint Height;
            public Rect2I Rect;
        }

        private struct SampleLevel
        {
            public int Region;
            public uint Width;
            public uint Height;
        }

        private struct Job
        {
            public int SourceRegion;
            public uint SourceWidth;
            public uint SourceHeight;
            public int TargetRegion;
            public uint TargetWidth;
            public uint TargetHeight;
            public float Radius;
            public int TapCount;
            public bool BarrierAfter;
        }

        private readonly GL _gl;
        private readonly GlBlurMipOptions _options;
        private static GlComputeProgram[]? _programs;

        private GlTexture? _texture;
        private Rect2I _sourceRect;

        private readonly List<Region> _regions = [];
        private readonly List<SampleLevel> _sampleLevels = [];
        private readonly List<Job> _jobs = [];
        private readonly int[] _scratchRegions = [-1, -1];

        private GlBlurMipLayout _layout;

        public GlBlurMipPack(GL gl, GlBlurMipOptions? options = null)
        {
            _gl = gl;
            _options = options ?? new GlBlurMipOptions();

            if (_programs == null)
            {
                _programs = new GlComputeProgram[3];

                _programs[0] = new GlComputeProgram(_gl, "Image/blur_mip.comp", Embedded.GetString<Material>);
                _programs[0].Build();

                _programs[1] = new GlComputeProgram(_gl, "Image/blur_mip.comp", Embedded.GetString<Material>);
                _programs[1].AddFeature("MULTI_VIEW");
                _programs[1].Build();

                _programs[2] = new GlComputeProgram(_gl, "Image/blur_mip.comp", Embedded.GetString<Material>);
                _programs[2].AddFeature("MULTI_SAMPLE");
                _programs[2].Build();
            }
        }

        public void Generate(GlTexture source, Rect2I sourceRect, float roughness)
        {
            Generate(source, sourceRect, false, Math.Clamp(roughness, 0, 1));
        }

        public void Generate(GlTexture source, Rect2I sourceRect)
        {
            Generate(source, sourceRect, true, 0);
        }

        private void Generate(GlTexture source, Rect2I sourceRect, bool dynamic, float roughness)
        {
            var curProgram = GlState.Current.ActiveProgram;

            var targetTextureTarget = source.Target == TextureTarget.Texture2DArray ? 
                TextureTarget.Texture2DArray : TextureTarget.Texture2D;

            var curTexture = GlState.Current.GetActiveTexture(source.Target, 0);
            var curTargetTexture = GlState.Current.GetActiveTexture(targetTextureTarget, 0);

#if DEBUG
            Validate(source, sourceRect);
#endif

            _sourceRect = sourceRect;

            if (dynamic)
                BuildDynamicPlan();
            else
                BuildStaticPlan(roughness);

            BuildPack(out var textureSize);

            var viewCount = source.Target == TextureTarget.Texture2DArray ? source.Depth : 1u;

            EnsureTexture(source, textureSize, viewCount);

#if DEBUG
            _texture!.Clear(Color.Transparent);
#endif
            BuildLayout(source, dynamic);
            Dispatch(source, viewCount);

            GlState.Current.SetActiveProgram(curProgram ?? 0);
            GlState.Current.LoadTexture(curTexture, source.Target, 0);
            GlState.Current.LoadTexture(curTargetTexture, _texture.Target, 0);
        }

        private void ResetPlan()
        {
            _regions.Clear();
            _sampleLevels.Clear();
            _jobs.Clear();

            _scratchRegions[0] = -1;
            _scratchRegions[1] = -1;
        }

        private void BuildStaticPlan(float roughness)
        {
            ResetPlan();

            if (roughness <= 0)
            {
                var targetRegion = AddRegion(_sourceRect.Width, _sourceRect.Height);

                AddJob(-1, _sourceRect.Width, _sourceRect.Height, targetRegion, _sourceRect.Width, _sourceRect.Height, 0, 1);

                _sampleLevels.Add(new SampleLevel
                {
                    Region = targetRegion,
                    Width = _sourceRect.Width,
                    Height = _sourceRect.Height
                });

                BuildBarriers();
                return;
            }

            var width = _sourceRect.Width;
            var height = _sourceRect.Height;
            var maxLod = MathF.Log2(Math.Max(width, height));
            var remainingLod = roughness * maxLod;

            var sourceRegion = -1;
            var scratch = 0;

            while (_jobs.Count + 1 < _options.MaxLevels)
            {
                var radius = BlurRadius(remainingLod);

                if (radius <= _options.MaxBlurRadius)
                    break;

                var nextWidth = Math.Max(1u, width / 2);
                var nextHeight = Math.Max(1u, height / 2);

                if (!CanDownsample(nextWidth, nextHeight))
                    break;

                var nextRemainingLod = Math.Max(0, remainingLod - 1);
                var nextFinalRadius = BlurRadius(nextRemainingLod);

                if (nextFinalRadius < _options.MinTerminalRadius)
                    break;

                var targetRegion = GetScratchRegion(scratch, nextWidth, nextHeight);

                AddJob(sourceRegion, width, height, targetRegion, nextWidth, nextHeight, 1, 4);

                sourceRegion = targetRegion;
                width = nextWidth;
                height = nextHeight;
                remainingLod = nextRemainingLod;
                scratch ^= 1;
            }

            var finalRadius = BlurRadius(remainingLod);
            var passCount = BlurPassCount(finalRadius);
            var passRadius = finalRadius / passCount;
            var tapCount = TapCount(passRadius);

            for (var i = 0; i < passCount; i++)
            {
                var targetRegion = GetScratchRegion(scratch, width, height);

                AddJob(sourceRegion, width, height, targetRegion, width, height, passRadius, tapCount);

                sourceRegion = targetRegion;
                scratch ^= 1;
            }

            _sampleLevels.Add(new SampleLevel
            {
                Region = sourceRegion,
                Width = width,
                Height = height
            });

            BuildBarriers();
        }

        private void BuildDynamicPlan()
        {
            ResetPlan();

            var levelCount = DynamicLevelCount();

            if (levelCount == 1)
            {
                BuildStaticPlan(1);
                return;
            }

            var maxLod = MathF.Log2(Math.Max(_sourceRect.Width, _sourceRect.Height));

            var level0 = AddRegion(_sourceRect.Width, _sourceRect.Height);

            _sampleLevels.Add(new SampleLevel
            {
                Region = level0,
                Width = _sourceRect.Width,
                Height = _sourceRect.Height
            });

            AddJob(-1, _sourceRect.Width, _sourceRect.Height, level0, _sourceRect.Width, _sourceRect.Height, 0, 1);

            var sourceRegion = level0;
            var sourceLod = 0.0f;
            var sourceWidth = _sourceRect.Width;
            var sourceHeight = _sourceRect.Height;

            for (var i = 1; i < levelCount; i++)
            {
                var roughness = i / (float)(levelCount - 1);
                var targetLod = roughness * maxLod;
                var targetSize = ComputeTargetSize(_sourceRect.Width, _sourceRect.Height, targetLod);
                var deltaLod = targetLod - sourceLod;
                var radius = BlurRadius(deltaLod);

                var targetRegion = AddRegion(targetSize.Width, targetSize.Height);

                _sampleLevels.Add(new SampleLevel
                {
                    Region = targetRegion,
                    Width = targetSize.Width,
                    Height = targetSize.Height
                });

                var passCount = BlurPassCount(radius);
                var passRadius = radius / passCount;
                var tapCount = TapCount(passRadius);

                if (passCount == 1)
                {
                    AddJob(sourceRegion, sourceWidth, sourceHeight, targetRegion, targetSize.Width, targetSize.Height, passRadius, tapCount);
                }
                else
                {
                    var currentRegion = sourceRegion;
                    var currentWidth = sourceWidth;
                    var currentHeight = sourceHeight;
                    var scratch = 0;

                    for (var pass = 0; pass < passCount; pass++)
                    {
                        var isLast = pass == passCount - 1;
                        var nextRegion = isLast ? targetRegion : GetScratchRegion(scratch, targetSize.Width, targetSize.Height);

                        AddJob(currentRegion, currentWidth, currentHeight, nextRegion, targetSize.Width, targetSize.Height, passRadius, tapCount);

                        currentRegion = nextRegion;
                        currentWidth = targetSize.Width;
                        currentHeight = targetSize.Height;

                        if (!isLast)
                            scratch ^= 1;
                    }
                }

                sourceRegion = targetRegion;
                sourceLod = targetLod;
                sourceWidth = targetSize.Width;
                sourceHeight = targetSize.Height;
            }

            BuildBarriers();
        }

        private int DynamicLevelCount()
        {
            if (_options.MaxLevels <= 1)
                return 1;

            var area = (ulong)_sourceRect.Width * _sourceRect.Height;
            var minArea = (ulong)_options.MinLevelSize * _options.MinLevelSize;

            if (area <= minArea)
                return 1;

            var count = 1 + (int)MathF.Ceiling(MathF.Log2(area / (float)minArea));

            return Math.Clamp(count, 2, _options.MaxLevels);
        }

        private int AddRegion(uint width, uint height)
        {
            _regions.Add(new Region
            {
                Width = width,
                Height = height
            });

            return _regions.Count - 1;
        }

        private int GetScratchRegion(int slot, uint width, uint height)
        {
            var index = _scratchRegions[slot];

            if (index == -1)
            {
                index = AddRegion(width, height);
                _scratchRegions[slot] = index;
                return index;
            }

            var region = _regions[index];
            region.Width = Math.Max(region.Width, width);
            region.Height = Math.Max(region.Height, height);
            _regions[index] = region;

            return index;
        }

        private void AddJob(int sourceRegion, uint sourceWidth, uint sourceHeight, int targetRegion, uint targetWidth, uint targetHeight, float radius, int tapCount)
        {
            _jobs.Add(new Job
            {
                SourceRegion = sourceRegion,
                SourceWidth = sourceWidth,
                SourceHeight = sourceHeight,
                TargetRegion = targetRegion,
                TargetWidth = targetWidth,
                TargetHeight = targetHeight,
                Radius = radius,
                TapCount = tapCount
            });
        }

        private void BuildBarriers()
        {
            for (var i = 0; i < _jobs.Count; i++)
            {
                var job = _jobs[i];

                if (i + 1 < _jobs.Count && _jobs[i + 1].SourceRegion == job.TargetRegion)
                    job.BarrierAfter = true;

                _jobs[i] = job;
            }
        }

        private Size2I ComputeTargetSize(uint width, uint height, float lod)
        {
            var steps = (int)MathF.Floor(lod);

            while (steps-- > 0)
            {
                var nextWidth = Math.Max(1u, width / 2);
                var nextHeight = Math.Max(1u, height / 2);

                if (!CanDownsample(nextWidth, nextHeight))
                    break;

                width = nextWidth;
                height = nextHeight;
            }

            return new Size2I(width, height);
        }

        private bool CanDownsample(uint width, uint height)
        {
            var area = (ulong)width * height;
            var minArea = (ulong)_options.MinLevelSize * _options.MinLevelSize;

            return area >= minArea;
        }

        private void BuildPack(out Size2I textureSize)
        {
            var order = new int[_regions.Count];

            for (var i = 0; i < order.Length; i++)
                order[i] = i;

            Array.Sort(order, (a, b) =>
            {
                var result = _regions[b].Height.CompareTo(_regions[a].Height);

                if (result == 0)
                    result = _regions[b].Width.CompareTo(_regions[a].Width);

                return result;
            });

            var minWidth = 0u;
            var maxWidth = 0u;

            for (var i = 0; i < _regions.Count; i++)
            {
                var width = _regions[i].Width + Padding * 2;

                minWidth = Math.Max(minWidth, width);
                maxWidth += width;
            }

            minWidth = Align(minWidth, _options.AllocationBlock);
            maxWidth = Align(maxWidth, _options.AllocationBlock);

            var bestArea = ulong.MaxValue;
            var bestSize = default(Size2I);
            Rect2I[]? bestRects = null;

            for (var width = minWidth; width <= maxWidth; width += _options.AllocationBlock)
            {
                var rects = new Rect2I[_regions.Count];

                var x = 0u;
                var y = 0u;
                var rowHeight = 0u;

                foreach (var index in order)
                {
                    var region = _regions[index];
                    var itemWidth = region.Width + Padding * 2;
                    var itemHeight = region.Height + Padding * 2;

                    if (x > 0 && x + itemWidth > width)
                    {
                        y += rowHeight;
                        x = 0;
                        rowHeight = 0;
                    }

                    rects[index] = new Rect2I(
                        (int)(x + Padding),
                        (int)(y + Padding),
                        region.Width,
                        region.Height);

                    x += itemWidth;
                    rowHeight = Math.Max(rowHeight, itemHeight);
                }

                var height = Align(y + rowHeight, _options.AllocationBlock);
                var area = (ulong)width * height;

                if (area >= bestArea)
                    continue;

                bestArea = area;
                bestSize = new Size2I(width, height);
                bestRects = rects;
            }

            for (var i = 0; i < _regions.Count; i++)
            {
                var region = _regions[i];
                region.Rect = bestRects![i];
                _regions[i] = region;
            }

            textureSize = bestSize;
        }

        [MemberNotNull(nameof(_texture))]
        private void EnsureTexture(GlTexture source, Size2I size, uint viewCount)
        {
            var width = Align(size.Width, _options.AllocationBlock);
            var height = Align(size.Height, _options.AllocationBlock);
            var format = source.InternalFormat.ToTextureFormat();

            if (format == TextureFormat.SRgba8)
                format = TextureFormat.Rgba8;

            _texture = GlTempAllocator.StaticTexture(_gl, width, height, viewCount, format, "blur");

            _texture.MinFilter = TextureMinFilter.Linear;
            _texture.MagFilter = TextureMagFilter.Linear;
            _texture.WrapS = TextureWrapMode.ClampToEdge;
            _texture.WrapT = TextureWrapMode.ClampToEdge;
        }

        private void BuildLayout(GlTexture source, bool dynamic)
        {
            var result = new Vector4[_sampleLevels.Count];

            for (var i = 0; i < _sampleLevels.Count; i++)
            {
                var level = _sampleLevels[i];
                var region = _regions[level.Region];

                result[i] = BuildUvTransform(source, new Rect2I(
                    region.Rect.X,
                    region.Rect.Y,
                    level.Width,
                    level.Height));
            }

            _layout = new GlBlurMipLayout(result, dynamic && result.Length > 1 ? result.Length - 1 : 0);
        }

        private Vector4 BuildUvTransform(GlTexture source, Rect2I target)
        {
            var targetScaleX = (target.Width - 1) / (float)_texture!.Width;
            var targetScaleY = (target.Height - 1) / (float)_texture.Height;

            var sourceScaleX = source.Width / (float)_sourceRect.Width;
            var sourceScaleY = source.Height / (float)_sourceRect.Height;

            var scaleX = sourceScaleX * targetScaleX;
            var scaleY = sourceScaleY * targetScaleY;

            var biasX = (target.X + 0.5f) / _texture.Width -
                        (_sourceRect.X / (float)_sourceRect.Width) * targetScaleX;

            var biasY = (target.Y + 0.5f) / _texture.Height -
                        (_sourceRect.Y / (float)_sourceRect.Height) * targetScaleY;

            return new Vector4(scaleX, scaleY, biasX, biasY);
        }

        private Rect2I GetRegionRect(int regionIndex, uint width, uint height)
        {
            var region = _regions[regionIndex];

            return new Rect2I(region.Rect.X, region.Rect.Y, width, height);
        }

        private void Dispatch(GlTexture source, uint viewCount)
        {
            var isMultiView = source.Target == TextureTarget.Texture2DArray;
            var isMultiSample = source.Target == TextureTarget.Texture2DMultisample;

            _gl.BindImageTexture(0, _texture!.Handle, 0, isMultiView, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba8);

            for (var i = 0; i < _jobs.Count; i++)
            {
                var job = _jobs[i];

                GlTexture sourceTexture;
                Rect2I sourceRect;

                if (job.SourceRegion == -1)
                {
                    sourceTexture = source;
                    sourceRect = _sourceRect;
                }
                else
                {
                    sourceTexture = _texture;
                    sourceRect = GetRegionRect(job.SourceRegion, job.SourceWidth, job.SourceHeight);
                }

                var useMultiSample = isMultiSample && job.SourceRegion == -1;
                var program = _programs![useMultiSample ? 2 : isMultiView ? 1 : 0];
                var targetRect = GetRegionRect(job.TargetRegion, job.TargetWidth, job.TargetHeight);

                program.Use();
                program.SetUniform("uSourceTexture", 0);

                if (useMultiSample)
                    program.SetUniform("uSampleCount", (int)source.SampleCount);

                GlState.Current.LoadTexture(sourceTexture, 0);

                program.SetUniform("uSourceRect", new Vector4(
                    sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height));

                program.SetUniform("uTargetRect", new Vector4(
                    targetRect.X, targetRect.Y, targetRect.Width, targetRect.Height));

                program.SetUniform("uRadius", job.Radius);
                program.SetUniform("uTapCount", job.TapCount);

                _gl.DispatchCompute((targetRect.Width + GroupSize - 1) / GroupSize,
                                    (targetRect.Height + GroupSize - 1) / GroupSize,
                                    viewCount);

                if (job.BarrierAfter)
                    _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
            }

            _gl.MemoryBarrier(MemoryBarrierMask.TextureFetchBarrierBit);
        }

        private int BlurPassCount(float radius)
        {
            return Math.Min(_options.MaxTerminalPasses, (int)MathF.Ceiling(radius / _options.MaxBlurRadius));
        }

        private static float BlurRadius(float lod)
        {
            if (lod <= 0)
                return 0;

            return (MathF.Pow(2, lod) - 1) * 0.5f;
        }

        private static int TapCount(float radius)
        {
            if (radius <= 0)
                return 1;

            if (radius <= 1)
                return 4;

            if (radius <= 4)
                return 8;

            return MaxTaps;
        }

        private static uint Align(uint value, uint block)
        {
            return (value + block - 1) / block * block;
        }

        private void Validate(GlTexture source, Rect2I sourceRect)
        {
            if (_options.MaxLevels < 1)
                throw new InvalidOperationException();

            if (_options.MinLevelSize == 0 || _options.AllocationBlock == 0 || _options.MaxBlurRadius <= 0)
                throw new InvalidOperationException();

            if (source.Target != TextureTarget.Texture2D &&
                source.Target != TextureTarget.Texture2DArray &&
                source.Target != TextureTarget.Texture2DMultisample)
            {
                throw new NotSupportedException($"Unsupported source target: {source.Target}");
            }

            if (sourceRect.Width == 0 || sourceRect.Height == 0)
                throw new ArgumentException(nameof(sourceRect));

            if (sourceRect.X < 0 || sourceRect.Y < 0 ||
                sourceRect.X + sourceRect.Width > source.Width ||
                sourceRect.Y + sourceRect.Height > source.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceRect));
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public GlTexture Texture => _texture!;

        public GlBlurMipLayout Layout => _layout;

    }
}