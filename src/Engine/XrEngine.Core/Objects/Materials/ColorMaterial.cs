using System.Globalization;
using XrMath;

namespace XrEngine
{
    public class ColorMaterial : ShaderMaterial, IColorSource
    {
        public static readonly Shader SHADER;

        static ColorMaterial()
        {
            SHADER = new StandardShader
            {
                FragmentSourceName = "color.frag",
                UseMotionVectors = true
            };
        }

        public ColorMaterial()
            : base()
        {
            _shader = SHADER;
            ShadowColor = new Color(0, 0, 0, 0.7f);
        }

        public ColorMaterial(Color color)
            : this()
        {
            Color = color;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<ColorMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject(this);
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (NormalScale > 0)
                bld.AddFeature($"NORMAL_SCALE {NormalScale.ToString("0.0#######", CultureInfo.InvariantCulture)}");

            if (Color.IsSrgb)
                bld.AddFeature("COLOR_IS_SRGB");

            bld.AddFeature("USE_INSTANCE", ctx => ctx.UseInstanceDraw, false);

            bld.AddFeature($"FRAG_LOCATION {Location}");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uColor", Color);
            });

            base.UpdateShaderMaterial(bld);
        }

        public float NormalScale { get; set; }

        public Color ShadowColor { get; set; }

        public Color Color { get; set; }

        public int Location { get; set; }
    }
}
