#if GLES
using Silk.NET.OpenGLES;
#else

using Silk.NET.OpenGL;
#endif

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrEngine.Helpers;
using XrMath;
using Common.Interop;
using System.Diagnostics;

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
            Debug.Assert(slot < MAX_SLOTS);

            var buffer = (GlBuffer<GlBlurMipLayout>)ctx.BufferProvider!.GetBuffer<GlBlurMipLayout>(
                UniformsSlots.BlurMip, BufferStore.Model, BufferUsage.Uniforms);

            if (buffer.SizeBytes == 0)
                buffer.Allocate((uint)MarshalCache.SizeOf(GetType()) * MAX_SLOTS, BufferAllocateFlags.None);

            buffer.UpdateRange([this], (int)slot, true);

            up.LoadBuffer(buffer, UniformsSlots.BlurMip, BufferUsage.Uniforms);
        }

        [FieldOffset(0)]
        public Vector4Array6 Levels;

        [FieldOffset(96)]
        public float LevelScale;

        public const int MAX_SLOTS = 4;
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
            public int Shift;
        }

        private struct Job
        {
            public int SourceRegion;
            public int SourceShift;
            public int TargetRegion;
            public int TargetShift;
            public float Radius;
            public int TapCount;
            public bool BarrierAfter;
        }

        private readonly struct PlanConfig
        {
            public PlanConfig(uint width, uint height, bool dynamic, float roughness, GlBlurMipOptions options)
            {
                Width = width;
                Height = height;
                Dynamic = dynamic;
                Roughness = dynamic ? 0 : roughness;
                MaxLevels = options.MaxLevels;
                MinLevelSize = options.MinLevelSize;
                MaxBlurRadius = options.MaxBlurRadius;
                AllocationBlock = options.AllocationBlock;
                MaxTerminalPasses = options.MaxTerminalPasses;
                MinTerminalRadius = options.MinTerminalRadius;

                var hash = HashBuilder.Instance;

                hash.Reset();
                hash.Add(Width);
                hash.Add(Height);
                hash.Add(Dynamic);
                hash.Add(Roughness);
                hash.Add(MaxLevels);
                hash.Add(MinLevelSize);
                hash.Add(MaxBlurRadius);
                hash.Add(AllocationBlock);
                hash.Add(MaxTerminalPasses);
                hash.Add(MinTerminalRadius);

                Key = hash.Value();
            }

            public bool Equals(in PlanConfig other)
            {
                return Key == other.Key;
            }

            public readonly ulong Key;
            public readonly uint Width;
            public readonly uint Height;
            public readonly bool Dynamic;
            public readonly float Roughness;
            public readonly int MaxLevels;
            public readonly uint MinLevelSize;
            public readonly float MaxBlurRadius;
            public readonly uint AllocationBlock;
            public readonly int MaxTerminalPasses;
            public readonly float MinTerminalRadius;
        }

        private sealed class Plan
        {
            public Plan(Region[] regions, SampleLevel[] sampleLevels, Job[] jobs, Size2I textureSize)
            {
                Regions = regions;
                SampleLevels = sampleLevels;
                Jobs = jobs;
                TextureSize = textureSize;
            }

            public readonly Region[] Regions;
            public readonly SampleLevel[] SampleLevels;
            public readonly Job[] Jobs;
            public readonly Size2I TextureSize;
        }

        private sealed class PlanBuilder
        {
            public readonly List<Region> Regions = [];
            public readonly List<SampleLevel> SampleLevels = [];
            public readonly List<Job> Jobs = [];
            public readonly int[] ScratchRegions = [-1, -1];
        }

        private readonly GL _gl;
        private readonly GlBlurMipOptions _options;

        private static GlComputeProgram[]? _programs;
        private static readonly Dictionary<ulong, Plan> _planCache = [];

        private GlTexture? _texture;
        private TextureFormat _textureFormat;
        private uint _textureViewCount;

        private Rect2I _sourceRect;

        private Plan? _plan;
        private PlanConfig _planConfig;
        private bool _hasPlan;

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
            var render = OpenGLRender.Current!;

            render.PushGroup("Generate Blur Mips");

            var curProgram = render.State.ActiveProgram;

            var targetTextureTarget = source.Target == TextureTarget.Texture2DArray ?
                TextureTarget.Texture2DArray : TextureTarget.Texture2D;

            var curTexture = render.State.GetActiveTexture(source.Target, 0);
            var curTargetTexture = render.State.GetActiveTexture(targetTextureTarget, 0);

#if DEBUG
            Validate(source, sourceRect);
#endif

            _sourceRect = sourceRect;

            var plan = GetPlan(sourceRect.Width, sourceRect.Height, dynamic, roughness);
            var viewCount = source.Target == TextureTarget.Texture2DArray ? source.Depth : 1u;

            EnsureTexture(source, plan.TextureSize, viewCount);

#if DEBUG
            _texture!.Clear(Color.Transparent);
#endif
            BuildLayout(source, plan, dynamic);
            Dispatch(source, plan, viewCount);

            render.State.SetActiveProgram(curProgram ?? 0);
            render.State.LoadTexture(curTexture, source.Target, 0);
            render.State.LoadTexture(curTargetTexture, _texture.Target, 0);

            render.PopGroup();
        }

        private Plan GetPlan(uint width, uint height, bool dynamic, float roughness)
        {
            var bucketWidth = Align(width, _options.AllocationBlock);
            var bucketHeight = Align(height, _options.AllocationBlock);

            var config = new PlanConfig(bucketWidth, bucketHeight, dynamic, roughness, _options);

            if (_hasPlan && _planConfig.Equals(config))
                return _plan!;

            var key = config.Key;

            if (!_planCache.TryGetValue(key, out _plan))
            {
                _plan = BuildPlan(config);
                _planCache[key] = _plan;
            }

            _planConfig = config;
            _hasPlan = true;

            return _plan;
        }

        private static Plan BuildPlan(in PlanConfig config)
        {
            var builder = new PlanBuilder();

            if (config.Dynamic)
                BuildDynamicPlan(builder, config);
            else
                BuildStaticPlan(builder, config);

            BuildBarriers(builder);

            var textureSize = BuildPack(builder, config.AllocationBlock);

            return new Plan(
                builder.Regions.ToArray(),
                builder.SampleLevels.ToArray(),
                builder.Jobs.ToArray(),
                textureSize);
        }

        private static void BuildStaticPlan(PlanBuilder builder, in PlanConfig config)
        {
            var width = config.Width;
            var height = config.Height;
            var maxLod = MathF.Log2(Math.Max(width, height));
            var remainingLod = config.Roughness * maxLod;

            var sourceRegion = GetScratchRegion(builder, 0, width, height);
            var sourceShift = 0;
            var scratch = 1;

            AddJob(builder, -1, 0, sourceRegion, 0, 0, 1);

            var downsampleCount = 0;

            while (downsampleCount + 1 < config.MaxLevels)
            {
                var radius = BlurRadius(remainingLod);

                if (radius <= config.MaxBlurRadius)
                    break;

                var nextWidth = Math.Max(1u, width / 2);
                var nextHeight = Math.Max(1u, height / 2);

                if (!CanDownsample(nextWidth, nextHeight, config.MinLevelSize))
                    break;

                var nextRemainingLod = Math.Max(0, remainingLod - 1);
                var nextFinalRadius = BlurRadius(nextRemainingLod);

                if (nextFinalRadius < config.MinTerminalRadius)
                    break;

                var targetRegion = GetScratchRegion(builder, scratch, nextWidth, nextHeight);

                AddJob(builder, sourceRegion, sourceShift, targetRegion, sourceShift + 1, 1, 4);

                sourceRegion = targetRegion;
                sourceShift++;
                width = nextWidth;
                height = nextHeight;
                remainingLod = nextRemainingLod;
                scratch ^= 1;
                downsampleCount++;
            }

            var finalRadius = BlurRadius(remainingLod);
            var passCount = BlurPassCount(finalRadius, config);

            if (passCount > 0)
            {
                var passRadius = finalRadius / passCount;
                var tapCount = TapCount(passRadius);

                for (var i = 0; i < passCount; i++)
                {
                    var targetRegion = GetScratchRegion(builder, scratch, width, height);

                    AddJob(builder, sourceRegion, sourceShift, targetRegion, sourceShift, passRadius, tapCount);

                    sourceRegion = targetRegion;
                    scratch ^= 1;
                }
            }

            builder.SampleLevels.Add(new SampleLevel
            {
                Region = sourceRegion,
                Shift = sourceShift
            });
        }

        private static void BuildDynamicPlan(PlanBuilder builder, in PlanConfig config)
        {
            var levelCount = DynamicLevelCount(config);
            var maxLod = MathF.Log2(Math.Max(config.Width, config.Height));

            var level0 = AddRegion(builder, config.Width, config.Height);

            builder.SampleLevels.Add(new SampleLevel
            {
                Region = level0,
                Shift = 0
            });

            AddJob(builder, -1, 0, level0, 0, 0, 1);

            if (levelCount == 1)
                return;

            var sourceRegion = level0;
            var sourceLod = 0.0f;
            var sourceShift = 0;

            for (var i = 1; i < levelCount; i++)
            {
                var roughness = i / (float)(levelCount - 1);
                var targetLod = roughness * maxLod;
                var targetShift = ComputeTargetShift(config.Width, config.Height, targetLod, config.MinLevelSize);
                var targetSize = GetLevelSize(config.Width, config.Height, targetShift);
                var deltaLod = targetLod - sourceLod;
                var radius = BlurRadius(deltaLod);

                var targetRegion = AddRegion(builder, targetSize.Width, targetSize.Height);

                builder.SampleLevels.Add(new SampleLevel
                {
                    Region = targetRegion,
                    Shift = targetShift
                });

                var passCount = BlurPassCount(radius, config);

                if (passCount <= 1)
                {
                    AddJob(builder, sourceRegion, sourceShift, targetRegion, targetShift, radius, TapCount(radius));
                }
                else
                {
                    var passRadius = radius / passCount;
                    var tapCount = TapCount(passRadius);
                    var currentRegion = sourceRegion;
                    var currentShift = sourceShift;
                    var scratch = 0;

                    for (var pass = 0; pass < passCount; pass++)
                    {
                        var isLast = pass == passCount - 1;
                        var nextRegion = isLast ? targetRegion : GetScratchRegion(builder, scratch, targetSize.Width, targetSize.Height);

                        AddJob(builder, currentRegion, currentShift, nextRegion, targetShift, passRadius, tapCount);

                        currentRegion = nextRegion;
                        currentShift = targetShift;

                        if (!isLast)
                            scratch ^= 1;
                    }
                }

                sourceRegion = targetRegion;
                sourceLod = targetLod;
                sourceShift = targetShift;
            }
        }

        private static int DynamicLevelCount(in PlanConfig config)
        {
            if (config.MaxLevels <= 1)
                return 1;

            var area = (ulong)config.Width * config.Height;
            var minArea = (ulong)config.MinLevelSize * config.MinLevelSize;

            if (area <= minArea)
                return 1;

            var count = 1 + (int)MathF.Ceiling(MathF.Log2(area / (float)minArea));

            return Math.Clamp(count, 2, config.MaxLevels);
        }

        private static int AddRegion(PlanBuilder builder, uint width, uint height)
        {
            builder.Regions.Add(new Region
            {
                Width = width,
                Height = height
            });

            return builder.Regions.Count - 1;
        }

        private static int GetScratchRegion(PlanBuilder builder, int slot, uint width, uint height)
        {
            var index = builder.ScratchRegions[slot];

            if (index == -1)
            {
                index = AddRegion(builder, width, height);
                builder.ScratchRegions[slot] = index;
                return index;
            }

            var region = builder.Regions[index];
            region.Width = Math.Max(region.Width, width);
            region.Height = Math.Max(region.Height, height);
            builder.Regions[index] = region;

            return index;
        }

        private static void AddJob(PlanBuilder builder, int sourceRegion, int sourceShift, int targetRegion, int targetShift, float radius, int tapCount)
        {
            builder.Jobs.Add(new Job
            {
                SourceRegion = sourceRegion,
                SourceShift = sourceShift,
                TargetRegion = targetRegion,
                TargetShift = targetShift,
                Radius = radius,
                TapCount = tapCount
            });
        }

        private static void BuildBarriers(PlanBuilder builder)
        {
            for (var i = 0; i < builder.Jobs.Count; i++)
            {
                var job = builder.Jobs[i];

                if (i + 1 < builder.Jobs.Count && builder.Jobs[i + 1].SourceRegion == job.TargetRegion)
                    job.BarrierAfter = true;

                builder.Jobs[i] = job;
            }
        }

        private static int ComputeTargetShift(uint width, uint height, float lod, uint minLevelSize)
        {
            var steps = (int)MathF.Floor(lod);
            var shift = 0;

            while (steps-- > 0)
            {
                var nextWidth = Math.Max(1u, width / 2);
                var nextHeight = Math.Max(1u, height / 2);

                if (!CanDownsample(nextWidth, nextHeight, minLevelSize))
                    break;

                width = nextWidth;
                height = nextHeight;
                shift++;
            }

            return shift;
        }

        private static Size2I GetLevelSize(uint width, uint height, int shift)
        {
            while (shift-- > 0)
            {
                width = Math.Max(1u, width / 2);
                height = Math.Max(1u, height / 2);
            }

            return new Size2I(width, height);
        }

        private static bool CanDownsample(uint width, uint height, uint minLevelSize)
        {
            var area = (ulong)width * height;
            var minArea = (ulong)minLevelSize * minLevelSize;

            return area >= minArea;
        }

        private static Size2I BuildPack(PlanBuilder builder, uint allocationBlock)
        {
            var order = new int[builder.Regions.Count];

            for (var i = 0; i < order.Length; i++)
                order[i] = i;

            Array.Sort(order, (a, b) =>
            {
                var result = builder.Regions[b].Height.CompareTo(builder.Regions[a].Height);

                if (result == 0)
                    result = builder.Regions[b].Width.CompareTo(builder.Regions[a].Width);

                return result;
            });

            var minWidth = 0u;
            var maxWidth = 0u;

            for (var i = 0; i < builder.Regions.Count; i++)
            {
                var width = builder.Regions[i].Width + Padding * 2;

                minWidth = Math.Max(minWidth, width);
                maxWidth += width;
            }

            minWidth = Align(minWidth, allocationBlock);
            maxWidth = Align(maxWidth, allocationBlock);

            var bestArea = ulong.MaxValue;
            var bestSize = default(Size2I);
            Rect2I[]? bestRects = null;

            for (var width = minWidth; width <= maxWidth; width += allocationBlock)
            {
                var rects = new Rect2I[builder.Regions.Count];

                var x = 0u;
                var y = 0u;
                var rowHeight = 0u;

                foreach (var index in order)
                {
                    var region = builder.Regions[index];
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

                var height = Align(y + rowHeight, allocationBlock);
                var area = (ulong)width * height;

                if (area >= bestArea)
                    continue;

                bestArea = area;
                bestSize = new Size2I(width, height);
                bestRects = rects;
            }

            for (var i = 0; i < builder.Regions.Count; i++)
            {
                var region = builder.Regions[i];
                region.Rect = bestRects![i];
                builder.Regions[i] = region;
            }

            return bestSize;
        }

        [MemberNotNull(nameof(_texture))]
        private void EnsureTexture(GlTexture source, Size2I size, uint viewCount)
        {
            var format = source.InternalFormat.ToTextureFormat();

            if (format == TextureFormat.SRgba8)
                format = TextureFormat.Rgba8;

            if (_texture != null &&
                _texture.Handle != 0 &&
                _texture.Width == size.Width &&
                _texture.Height == size.Height &&
                _textureFormat == format &&
                _textureViewCount == viewCount)
            {
                return;
            }

            _texture = GlTempAllocator.StaticTexture(_gl, size.Width, size.Height, viewCount, format, "blur");

            _texture.MinFilter = TextureMinFilter.Linear;
            _texture.MagFilter = TextureMagFilter.Linear;
            _texture.WrapS = TextureWrapMode.ClampToEdge;
            _texture.WrapT = TextureWrapMode.ClampToEdge;

            _textureFormat = format;
            _textureViewCount = viewCount;
        }

        private void BuildLayout(GlTexture source, Plan plan, bool dynamic)
        {
            _layout = default;

            for (var i = 0; i < plan.SampleLevels.Length; i++)
            {
                var level = plan.SampleLevels[i];
                var region = plan.Regions[level.Region];
                var size = GetLevelSize(_sourceRect.Width, _sourceRect.Height, level.Shift);

                _layout.Levels[i] = BuildUvTransform(source, new Rect2I(
                    region.Rect.X,
                    region.Rect.Y,
                    size.Width,
                    size.Height));
            }

            _layout.LevelScale = dynamic && plan.SampleLevels.Length > 1 ? plan.SampleLevels.Length - 1 : 0;
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

        private Rect2I GetRegionRect(Plan plan, int regionIndex, int shift)
        {
            var region = plan.Regions[regionIndex];
            var size = GetLevelSize(_sourceRect.Width, _sourceRect.Height, shift);

            return new Rect2I(region.Rect.X, region.Rect.Y, size.Width, size.Height);
        }

        private void Dispatch(GlTexture source, Plan plan, uint viewCount)
        {
            var isMultiView = source.Target == TextureTarget.Texture2DArray;
            var isMultiSample = source.Target == TextureTarget.Texture2DMultisample;

            _gl.BindImageTexture(0, _texture!.Handle, 0, isMultiView, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba8);

            for (var i = 0; i < plan.Jobs.Length; i++)
            {
                var job = plan.Jobs[i];

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
                    sourceRect = GetRegionRect(plan, job.SourceRegion, job.SourceShift);
                }

                var useMultiSample = isMultiSample && job.SourceRegion == -1;
                var program = _programs![useMultiSample ? 2 : isMultiView ? 1 : 0];
                var targetRect = GetRegionRect(plan, job.TargetRegion, job.TargetShift);

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

        private static int BlurPassCount(float radius, in PlanConfig config)
        {
            if (radius <= 0)
                return 0;

            return Math.Min(config.MaxTerminalPasses, Math.Max(1, (int)MathF.Ceiling(radius / config.MaxBlurRadius)));
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
            if (_options.MaxLevels < 1 || _options.MaxLevels > 6)
                throw new InvalidOperationException();

            if (_options.MinLevelSize == 0 ||
                _options.AllocationBlock == 0 ||
                _options.MaxBlurRadius <= 0 ||
                _options.MaxTerminalPasses < 1)
            {
                throw new InvalidOperationException();
            }

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

            var srcFormat = source.InternalFormat.ToTextureFormat();

            if (srcFormat != TextureFormat.Rgba8 && srcFormat != TextureFormat.SRgba8)
                throw new NotSupportedException();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public GlTexture Texture => _texture!;

        public GlBlurMipLayout Layout => _layout;

    }
}