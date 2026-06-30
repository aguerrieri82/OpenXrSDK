using XrMath;

namespace XrEngine
{
    public enum GlowAttType
    {
        Width = 0,
        Point = 1
    }

    public class GlowSphereMaterial : ShaderMaterial, IColorSource
    {
        public static readonly Shader SHADER;

        static GlowSphereMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "glow.frag",
                IsLit = false
            };
        }


        public GlowSphereMaterial()
            : base()
        {
            _shader = SHADER;

            DoubleSided = true;
            WriteDepth = false;
            Alpha = AlphaMode.Blend;
            Intensity = 1;
            Width = 0.01f;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uCenter", ctx.Model!.WorldPosition);
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
            });
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature($"ATTENUATION_TYPE {(int)Attenuation}");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uColor", Color);
                up.SetUniform("uWidth", Width);
                up.SetUniform("uIntensity", Intensity);
                up.SetUniform("uRadius", Radius);
            });
        }
        

        public GlowAttType Attenuation { get; set; }


        public Color Color { get; set; }

        [Range(0, 1, 0.001f)]
        public float Radius { get; set; }

        [Range(0, 1, 0.001f)]
        public float Intensity { get; set; }

        [Range(0, 1, 0.001f)]
        public float Width { get; set; }
    }
}
