namespace OpenXr.Engine
{
    public class TextureMaterial : ShaderMaterial, IShaderHandler
    {
        static readonly Shader SHADER;

        static TextureMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "texture.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }


        public TextureMaterial()
            : base()
        {
            _shader = SHADER;
        }


        public TextureMaterial(Texture2D texture)
            : this()
        {
            Texture = texture;
        }

        public override void UpdateShader(UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl)
        {
            up.SetUniform("uTexture0", Texture!, 0);

            ctx.UpdateStandardVS(up, fl);
        }

        public Texture2D? Texture { get; set; }
    }
}
