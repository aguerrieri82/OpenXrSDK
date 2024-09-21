namespace XrEngine.OpenGL
{
    public enum ShaderPrecision
    {
        Low,
        Medium,
        High
    }



    public class GlRenderOptions
    {
        public GlRenderOptions()
        {
            FloatPrecision = ShaderPrecision.High;
            ShaderVersion = "310 es";
            FrustumCulling = true;
            UseOcclusionQuery = true;
            UseDepthPass = false;
            UseSRGB = false;
            ShadowMap = new ShadowMapOptions()
            {
                Mode = ShadowMapMode.HardSmooth,
                Size = 2048,
            };  
        }

        public bool UseSRGB { get; set; }

        public string? ShaderVersion { get; set; }

        public ShaderPrecision FloatPrecision { get; set; }

        public bool RequireTextureCompression { get; set; }

        public bool FrustumCulling { get; set; }

        public bool UseOcclusionQuery { get; set; }

        public bool UseDepthPass { get; set; }

        public ShadowMapOptions ShadowMap { get; }

        public static GlRenderOptions Default() => new();

    }
}
