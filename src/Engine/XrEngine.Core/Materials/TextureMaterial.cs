namespace XrEngine
{
    public class TextureMaterial : ShaderMaterial
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

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.SetUniform("uModel", (ctx) => ctx.Model!.WorldMatrix);

            bld.SetUniform("uTexture0", (ctx) => Texture!, 0);
        }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;

        public Texture2D? Texture { get; set; }
    }
}
