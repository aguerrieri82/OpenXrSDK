using XrMath;

namespace XrEngine
{
    public class ColorMaterial : ShaderMaterial, IColorSource
    {
        static readonly Shader SHADER;

        static ColorMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "color.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }


        public ColorMaterial()
            : base()
        {
            _shader = SHADER;
            ShadowIntensity = 0.7f;
        }

        public ColorMaterial(Color color)
            : this()
        {
            Color = color;
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.SetUniform("uModel", (ctx) => ctx.Model!.WorldMatrix);
            bld.SetUniform("uColor", ctx => Color);
        }

        public float ShadowIntensity { get; set; }  

        public bool IsShadowOnly { get; set; }

        public Color Color { get; set; }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;
    }
}
