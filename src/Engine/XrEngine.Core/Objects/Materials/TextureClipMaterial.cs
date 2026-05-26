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
            Color = Color.White;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
            });
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uColor", Color);
                if (Texture != null)
                    up.LoadTexture(Texture, 0);
            });
        }

        public Texture2D? Texture { get; set; }

        public Color Color { get; set; }

    }
}
