using XrMath;

namespace XrEngine
{

    [Flags]
    public enum GlowFadeMode
    {
        Linear = 1,
        Exp = 2,
        Both = Linear | Exp
    };

    public class GlowVolumeMaterial : ShaderMaterial, IVolumeMaterial
    {
        public static readonly Shader SHADER;


        static GlowVolumeMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "glow_vol.frag",
                IsLit = false
            };
        }


        public GlowVolumeMaterial()
            : base()
        {
            _shader = SHADER;

            DoubleSided = true;
            StepSize = 0.1f;
            Alpha = AlphaMode.Blend;
            UseDepth = false;
            WriteDepth = false;
            FadeMode = GlowFadeMode.Linear;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("sphereCenter", ctx.Model!.WorldPosition);
            });
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("sphereRadius", SphereRadius);
                up.SetUniform("haloWidth", HaloWidth);
                up.SetUniform("haloColor", HaloColor);
                up.SetUniform("stepSize", StepSize);

            });

            if (UseDepthCulling)
            {
                bld.AddFeature("USE_DEPTH_CULL");
                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uInvViewProj", ctx.PassCamera!.ViewProjectionInverse);
                    up.LoadTexture(new Texture2D() { Type = TextureType.Depth }, 1);
                });
            }

            bld.AddFeature($"FADE_MODE {(int)FadeMode}");

            if (BlendColor)
                bld.AddFeature("BLEND_COLOR");
        }

        public bool BlendColor { get; set; }

        public bool UseDepthCulling { get; set; }

        public float SphereRadius { get; set; }

        public float HaloWidth { get; set; }

        public Color HaloColor { get; set; }

        [Range(0, 1, 0.001f)]
        public float StepSize { get; set; }

        public GlowFadeMode FadeMode { get; set; }
    }
}