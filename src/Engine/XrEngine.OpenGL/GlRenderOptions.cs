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
    }


    public class GlRenderOptions
    {
        public GlRenderOptions()
        {
            FloatPrecision = ShaderPrecision.High;
            ShaderVersion = "320 es";
            FrustumCulling = true;
            UseOcclusionQuery = true;
            UseDepthPass = false;
            SortByCameraDistance = true;
            UseSRGB = false;
            RequireTextureCompression = false;
            ShadowMap = new ShadowMapOptions()
            {
                Mode = ShadowMapMode.PCF,
                Size = 2048,
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

        public ShaderPrecision FloatPrecision { get; set; }

        public bool RequireTextureCompression { get; set; }

        public bool FrustumCulling { get; set; }

        public bool UseOcclusionQuery { get; set; }

        public bool UseDepthPass { get; set; }

        public bool UsePlanarReflection { get; set; }

        public bool UseHitTest { get; set; }

        public ShadowMapOptions ShadowMap { get; }

        public GlOutlineOptions Outline { get; }

        public bool SortByCameraDistance { get; set; }

        public static GlRenderOptions Default() => new();

    }
}
