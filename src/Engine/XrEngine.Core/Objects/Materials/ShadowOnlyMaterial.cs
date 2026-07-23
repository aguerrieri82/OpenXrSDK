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
            var options = bld.Context.ShadowMapProvider!.Options;

            bld.AddFeature("SHADOW_MAP_MODE " + (int)options.Mode);

            bld.AddFeature("SHADOW_BIAS " + (int)options.BiasMode);

            if (options.UseShadowSampler)
                bld.AddFeature("USE_SHADOW_SAMPLER");

            if (ShadowColor.IsSrgb)
                bld.AddFeature("COLOR_IS_SRGB");

            bld.ExecuteAction((ctx, up) =>
            {
                if (bld.Context.ShadowMapProvider.ShadowMap != null)
                    up.LoadTexture(bld.Context.ShadowMapProvider.ShadowMap, TextureSlots.ShadowMap);

                up.SetUniform("uShadowColor", ShadowColor);

                if (options?.BiasMode == ShadowMapBiasMode.Value)
                    up.SetUniform("uShadowBias", options!.Bias);

                if (options?.Mode == ShadowMapMode.VSM)
                    up.SetUniform("uLightBleed", options!.LightBleed);
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
