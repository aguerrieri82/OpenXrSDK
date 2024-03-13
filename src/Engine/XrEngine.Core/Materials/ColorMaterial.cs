namespace XrEngine
{
    public class ColorMaterial : ShaderMaterial
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

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;
    }
}
