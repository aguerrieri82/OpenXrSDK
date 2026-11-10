using XrEngine;

namespace XrSamples.Graffiti
{
    public class CapPreviewMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        private Can? _can;

        static CapPreviewMaterial()
        {
            SHADER = new StandardShader
            {
                FragmentSourceName = "cap_preview.frag",
                IsLit = false,
                Resolver = str => str == "cap_preview.frag" ?
                    Embedded.GetString<CapPreviewMaterial>(str) :
                    Embedded.GetString<StandardShader>(str)
            };
        }

        public CapPreviewMaterial()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Blend;
            DoubleSided = true;
            Extent = 0.3f;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (IsSide)
                bld.AddFeature("SIDE");
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                _can ??= ctx.Model?.Scene?.Descendants<Can>().First();

                if (_can == null)
                    return;

                var cap = _can.Cap;

                up.SetUniform("uNozzleRadius", cap.SprayRadius * _can.Transform.Scale.X);
                up.SetUniform("uTanAngle", MathF.Tan(cap.SpreadAngle));
                up.SetUniform("uDistanceK", MathF.Sin(cap.SpreadAngle) / cap.SprayRadius);
                up.SetUniform("uRadialFalloff", cap.RadialFalloff);
                up.SetUniform("uDistance", Distance);
                up.SetUniform("uMaskSize", cap.MaskSize);
                up.SetUniform("uMaskAngle", cap.MaskAngle);
                up.SetUniform("uExtent", Extent);
                up.SetUniform("uDensity", cap.BaseDensity);
            });

            base.UpdateShaderModel(bld);
        }

        public float Distance { get; set; }

        public float Extent { get; set; }

        public bool IsSide { get; set; }
    }
}
