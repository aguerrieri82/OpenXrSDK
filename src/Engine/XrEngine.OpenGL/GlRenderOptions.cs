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
        }

        public bool UseSRGB { get; set; }

        public string? ShaderVersion { get; set; }

        public ShaderPrecision FloatPrecision { get; set; }

        public bool RequireTextureCompression { get; set; }

        public static GlRenderOptions Default() => new();

    }
}
