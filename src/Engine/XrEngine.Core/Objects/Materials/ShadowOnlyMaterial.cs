using XrMath;

namespace XrEngine.Objects
{
    public class ShadowOnlyMaterial : ShaderMaterial, IShadowMaterial
    {
        static readonly Shader SHADER;

        static ShadowOnlyMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "shadow_only.frag",
                IsLit = false,
            };
        }

        public ShadowOnlyMaterial()
            : base()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Blend;
            ShadowColor = new Color(0, 0, 0, 0.7f);
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            var mode = bld.Context.ShadowMapProvider!.Options.Mode;

            bld.ExecuteAction((ctx, up) =>
            {
                if (bld.Context.ShadowMapProvider.ShadowMap != null)
                    up.LoadTexture(bld.Context.ShadowMapProvider.ShadowMap, 14);

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
