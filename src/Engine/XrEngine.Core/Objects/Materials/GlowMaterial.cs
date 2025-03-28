using XrMath;

namespace XrEngine
{
    public class GlowMaterial : ShaderMaterial, IColorSource
    {
        public static readonly Shader SHADER;

        static GlowMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "glow.frag",
                IsLit = false
            };
        }


        public GlowMaterial()
            : base()
        {
            _shader = SHADER;

            DoubleSided = true;
            Intensity = 1;
            Width = 0.01f;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uCenter", ctx.Model!.WorldPosition);
            });
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uCenter", ctx.Model!.WorldPosition);
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uColor", Color);
                up.SetUniform("uWidth", Width);
                up.SetUniform("uIntensity", Intensity);
                up.SetUniform("uRadius", Radius);
            });
        }


        public Color Color { get; set; }

        [Range(0, 1, 0.001f)]
        public float Radius { get; set; }

        [Range(0, 1, 0.001f)]
        public float Intensity { get; set; }

        [Range(0, 1, 0.001f)]
        public float Width { get; set; }
    }
}
