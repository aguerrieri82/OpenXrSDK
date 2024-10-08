using XrMath;

namespace XrEngine.Objects
{
    public class ShadowOnlyMaterial : ShaderMaterial, IShadowMaterial
    {
        static readonly Shader SHADER;

        static ShadowOnlyMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "shadow_only.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                UpdateHandler = StandardVertexShaderHandler.Instance
            };
        }

        public ShadowOnlyMaterial()
            : base()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Blend;
            ShadowColor = new Color(0, 0, 0, 0.7f);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            var mode = bld.Context.ShadowMapProvider!.Options.Mode;

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uShadowColor", ShadowColor);
            });
        }

        public Color ShadowColor { get; set; }

        bool IShadowMaterial.ReceiveShadows
        {
            get => true;
            set
            {
                if (!value)
                    throw new NotSupportedException();
            }
        }

    }
}
