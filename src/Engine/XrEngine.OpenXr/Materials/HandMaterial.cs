using System.Globalization;
using XrMath;

namespace XrEngine.OpenXr
{
    public class HandMaterial : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static HandMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "[XrEngine.OpenXr]hand.frag",
                IsLit = false
            };
        }


        public HandMaterial()
            : base()
        {
            _shader = SHADER;

        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("FRAG_RAW_POS");
            bld.AddFeature($"NORMAL_SCALE {NormalScale.ToString("0.0#######", CultureInfo.InvariantCulture)}");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uColor", Color);
                up.SetUniform("uFadeEnd", FadeEnd);
                up.SetUniform("uFadeStart", FadeStart);
                up.SetUniform("uFadeSide", FadeSide);
            });

            base.UpdateShaderMaterial(bld);
        }


        public Color Color { get; set; }

        [Range(0, 0.5f, 0.001f)]
        public float FadeEnd { get; set; }

        [Range(0, 0.5f, 0.001f)]
        public float FadeStart { get; set; }

        public float FadeSide { get; set; }


        [Range(0, 0.01f, 0.001f)]
        public float NormalScale { get; set; }
    }
}
