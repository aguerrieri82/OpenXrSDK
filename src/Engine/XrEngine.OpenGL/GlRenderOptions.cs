using System.Numerics;
using XrMath;

namespace XrEngine.OpenGL
{
    public enum ShaderPrecision
    {
        Low,
        Medium,
        High
    }

    public class GlOutlineOptions
    {
        public bool Use { get; set; }

        public Color Color { get; set; }

        public float Size { get; set; }

        public bool IsMultiView { get; set; }

        public float DownsampleFactor { get; set; }
    }

    public class GlCompressionOptions
    {
        public bool Use { get; set; }

        public TextureCompressionFormat Format { get; set; }

        public int MinSize { get; set; }

        public uint BlockSize { get; set; }

        public float Quality { get; set; }
    }

    public class GlRenderOptions
    {
        public GlRenderOptions()
        {
            FloatPrecision = ShaderPrecision.High;
            IntPrecision = ShaderPrecision.High;
            SamplerPrecision = ShaderPrecision.High;
            ShaderVersion = "320 es";
            FrustumCulling = true;
            UseOcclusionQuery = false;
            UseDepthPass = false;
            SortByCameraDistance = true;
            UseSRGB = true;
            RequireTextureCompression = true;
            UseVolume = true;
            SampleCount = 4;
            UseInstanceDraw = true;
            CacheUniforms = true;
            ToneMap = ToneMapMode.Neutral;
            UseResolve = false;
            UseAsyncShaderCompile = true;
            UseShaderCache = true;
            UseShaderPreprocessor = true;
            UseRayCollider = true;
            ContactShadow = new()
            {
                Use = false,
                MaxDistance = 0.12f,
                Thickness = 0.015f,
                Strength = 0.65f,
                StepCount = 6.0f,
                DepthBias = 0.0005f,
                FadeDistance = 0.12f,
                ApplyStrength = 1.0f
            };
            Compression = new GlCompressionOptions
            {
                Use = false,
                MinSize = 512 * 512,
                BlockSize = 4,
                Format = TextureCompressionFormat.Astc,
                Quality = 60,
            };
            ShadowMap = new ShadowMapOptions()
            {
                Mode = ShadowMapMode.PCF,
                Bias = 0.001f,
                BiasMode = ShadowMapBiasMode.Value,
                Size = 2048,
                LightBleed = 0.15f,
                BlurRadius = 2,
                IsCasterMode = false,
                UseFrustumIntersect = false,
                UseShadowSampler = true,
                Expand = new Vector3(0.1f, 0.1f, 0.1f)
            };
            Outline = new GlOutlineOptions()
            {
                Use = false,
                Color = new Color(1, 1, 0, 0.7f),
                Size = 2,
                DownsampleFactor = 1f
                //DownsampleFactor = 1.5f
            };


        }

        public bool UseSRGB { get; set; }

        public string? ShaderVersion { get; set; }

        public ContactShadowOptions ContactShadow { get; set; }

        public GlCompressionOptions Compression { get; set; }

        public ShaderPrecision SamplerPrecision { get; set; }

        public ShaderPrecision FloatPrecision { get; set; }

        public ShaderPrecision IntPrecision { get; set; }

        public bool RequireTextureCompression { get; set; }

        public bool FrustumCulling { get; set; }

        public bool UseOcclusionQuery { get; set; }

        public bool UseDepthPass { get; set; }

        public bool UsePlanarReflection { get; set; }

        public bool UseVolume { get; set; }

        public uint SampleCount { get; set; }

        public bool UseHitTest { get; set; }

        public ShadowMapOptions ShadowMap { get; }

        public GlOutlineOptions Outline { get; }

        public bool SortByCameraDistance { get; set; }

        public bool UseInstanceDraw { get; set; }

        public bool CacheUniforms { get; set; }

        public bool InvalidateDepth { get; set; }

        public ToneMapMode ToneMap { get; set; }

        public bool UseResolve { get; set; }

        public bool UseHighQualitySrgb { get; set; }

        public bool UseAsyncShaderCompile { get; set; }

        public bool UseShaderCache { get; set; }

        public bool UseShaderPreprocessor { get; set; }

        public bool UseRayCollider { get; set; }

        public static GlRenderOptions Default() => new();

    }
}
