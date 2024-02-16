namespace OpenXr.Engine.OpenGL
{
    public enum ShaderPrecision
    {
        Low,
        Medium,
        High
    }

    public enum GlftExtensions
    {
        KHR_materials_clearcoat,
        KHR_materials_sheen,
        KHR_materials_transmission,
        KHR_materials_volume,
        KHR_materials_ior,
        KHR_materials_specular,
        KHR_materials_iridescence,
        KHR_materials_emissive_strength,
        KHR_materials_anisotropy,
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
