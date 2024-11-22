using XrMath;

namespace XrEngine
{
    public class TextureClipMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static TextureClipMaterial()
        {
            SHADER = new Shader
            {
                Resolver = str => Embedded.GetString(str),
                VertexSourceName = "clip.vert",
                FragmentSourceName = "texture.frag",
                IsLit = false,
            };
        }

        public TextureClipMaterial()
        {
            Shader = SHADER;
            UseDepth = true;
            WriteDepth = true;
        }


        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uColor", Color.White);
                if (Texture != null)
                    up.LoadTexture(Texture, 0);
            });
        }

        public Texture2D? Texture { get; set; }

    }
}
