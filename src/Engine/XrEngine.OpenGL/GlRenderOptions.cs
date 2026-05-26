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
            ShaderVersion = "320 es";
            FrustumCulling = true;
            UseOcclusionQuery = false;
            UseDepthPass = false;
            SortByCameraDistance = true;
            UseSRGB = false;
            UseLayerV2 = true;
            RequireTextureCompression = true;
            UseVolume = true;
            SampleCount = 4;
            UseInstanceDraw = true;
            CacheUniforms = true;
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
                Mode = ShadowMapMode.VSM,
                Bias = 0,
                BiasMode = ShadowMapBiasMode.Auto,
                Size = 2048,
                LightBleed = 0.1f,
                BlurRadius = 10,
                IsCasterMode = false,
                UseFrustumIntersect = false,
                Expand = new Vector3(0.1f, 0.1f, 0.1f)
            };
            Outline = new GlOutlineOptions()
            {
                Use = false,
                Color = new Color(1, 1, 0, 0.7f),
                Size = 2
            };

            /*
            PbrMaterial.LinearOutput = true;
            PbrMaterial.ToneMap = PbrMaterial.ToneMapType.TONEMAP_KHR_PBR_NEUTRAL;
            */
        }

        public bool UseSRGB { get; set; }

        public string? ShaderVersion { get; set; }

        public GlCompressionOptions Compression { get; set; }

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

        public bool UseLayerV2 { get; set; }

        public bool UseInstanceDraw { get; set; }

        public bool CacheUniforms { get; set; }

        public static GlRenderOptions Default() => new();

    }
}
