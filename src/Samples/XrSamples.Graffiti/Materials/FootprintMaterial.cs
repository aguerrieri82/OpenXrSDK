using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class FootprintMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        private Can? _can;
        private SprayTracker? _sprayTracker;

        static FootprintMaterial()
        {
            SHADER = new StandardShader
            {
                FragmentSourceName = "footprint.frag",
                IsLit = false,
                Resolver = str => str == "footprint.frag" ?
                    Embedded.GetString<FootprintMaterial>(str) :
                    Embedded.GetString<StandardShader>(str)
            };
        }

        public FootprintMaterial()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Blend;
            DoubleSided = true;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                _can ??= ctx.Model?.Scene?.Descendants<Can>().First();

                if (_can == null)
                    return;

                _sprayTracker ??= _can.Component<SprayTracker>();

                up.SetUniform("uPaintColor", _can.Color.ToVector3());
                up.SetUniform("uNozzleWorld", _sprayTracker.SprayCenter.Transform(_can.WorldMatrix));
                up.SetUniform("uRadialFalloff", _sprayTracker.RadialFalloff);
                up.SetUniform("uDistanceK", MathF.Sin(_sprayTracker.SpreadAngle) / _sprayTracker.SprayRadius);
                up.SetUniform("uDensity", _sprayTracker.BaseDensity);
                up.SetUniform("uMaskSize", _sprayTracker.MaskSize);
                up.SetUniform("uMaskAngle", _sprayTracker.MaskAngle);
            });

            base.UpdateShaderModel(bld);
        }
    }
}
