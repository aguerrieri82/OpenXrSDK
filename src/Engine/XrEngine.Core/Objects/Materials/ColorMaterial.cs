using XrMath;

namespace XrEngine
{
    public class ColorMaterial : ShaderMaterial, IColorSource
    {
        public static readonly Shader SHADER;

        static ColorMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "color.frag",
                IsLit = false
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
            container.ReadObject<ColorMaterial>(this);
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uColor", Color);
            });
        }


        public Color ShadowColor { get; set; }

        public Color Color { get; set; }
    }
}
