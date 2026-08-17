using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class GlowConeMaterial : ShaderMaterial, IColorSource
    {
        public static readonly Shader SHADER;

        static GlowConeMaterial()
        {
            SHADER = new StandardShader
            {
                FragmentSourceName = "glow_cone.frag",
                IsLit = false
            };
        }

        public GlowConeMaterial()
            : base()
        {
            _shader = SHADER;

            DoubleSided = true;
            WriteDepth = false;
            Alpha = AlphaMode.Blend;

            Color = "#ffffff";
            Intensity = 1f;
            Range = 1f;
            InnerAngle = MathF.PI * 0.15f;
            OuterAngle = MathF.PI * 0.20f;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                var model = ctx.Model!;

                var dir = Vector3.TransformNormal(Vector3.UnitY, model.WorldMatrix);

                if (dir.LengthSquared() > 1e-6f)
                    dir = Vector3.Normalize(dir);

                up.SetUniform("uCenter", model.WorldPosition - dir * (Range * 0.5f));
                up.SetUniform("uDirection", dir);
                up.SetUniform("uNormalMatrix", model.NormalMatrix);
                up.SetUniform("uModel", model.WorldMatrix);
            });
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uColor", Color);
                up.SetUniform("uIntensity", Intensity);
                up.SetUniform("uRange", Range);
                up.SetUniform("uInnerAngle", InnerAngle);
                up.SetUniform("uOuterAngle", OuterAngle);
            });
        }

        public Color Color { get; set; }

        [Range(0, 100, 0.001f)]
        public float Range { get; set; }

        [Range(0, 100, 0.001f)]
        public float Intensity { get; set; }

        [ValueType(ValueType.Radiant)]
        public float InnerAngle { get; set; }

        [ValueType(ValueType.Radiant)]
        public float OuterAngle { get; set; }
    }
}