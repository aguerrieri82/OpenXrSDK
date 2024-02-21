namespace OpenXr.Engine
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

        public override void UpdateShader(UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl)
        {
            up.SetUniform("uColor", Color);
            ctx.UpdateStandardVS(up, fl);
        }

    }
}
