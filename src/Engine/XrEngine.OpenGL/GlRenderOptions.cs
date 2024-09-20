namespace XrEngine.OpenGL
{
    public enum ShaderPrecision
    {
        Low,
        Medium,
        High
    }

    public class GlShadowMapOptions
    {
        public bool Use { get; set; }  

        public uint Size { get; set; }

        public bool Smooth { get; set; }   
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
            ShadowMap = new GlShadowMapOptions()
            {
                Use = false,
                Size = 2048,
                Smooth = true
            };  
        }

        public bool UseSRGB { get; set; }

        public string? ShaderVersion { get; set; }

        public ShaderPrecision FloatPrecision { get; set; }

        public bool RequireTextureCompression { get; set; }

        public bool FrustumCulling { get; set; }

        public bool UseOcclusionQuery { get; set; }

        public bool UseDepthPass { get; set; }

        public GlShadowMapOptions ShadowMap { get; }

        public static GlRenderOptions Default() => new();

    }
}
