namespace OpenXr.Engine.OpenGL
{
    public enum ShaderPrecision
    {
        Low,
        Medium,
        High
    }

    public class GlRenderOptions
    {
        public string? ShaderVersion { get; set; }

        public ShaderPrecision FloatPrecision { get; set; }

        public IList<string>? ShaderExtensions { get; set; }


        public static readonly GlRenderOptions Default = new GlRenderOptions
        {
            FloatPrecision = ShaderPrecision.Medium,
            ShaderVersion = "300 es"
        };

    }
}
