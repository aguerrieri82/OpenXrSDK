namespace OpenXr.Engine
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


        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("uTexture0", Texture!, 0);
        }

        public Texture2D? Texture { get; set; }
    }
}
