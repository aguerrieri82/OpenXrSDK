using System.Numerics;

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
                IsLit = false,
                UpdateHandler = StandardVertexShaderHandler.Instance
            };
        }
        public TextureMaterial()
            : base()
        {
            _shader = SHADER;
            Scale = new Vector2(1, 1);
        }

        public TextureMaterial(Texture2D texture)
            : this()
        {
            Texture = texture;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<TextureMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<TextureMaterial>(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (Texture?.Type == TextureType.External)
            {
                bld.AddExtension("GL_OES_EGL_image_external_essl3");
                bld.AddFeature("EXTERNAL");
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uTexture", Texture!, 0);
                up.SetUniform("uOffset", Offset);
                up.SetUniform("uScale", Scale);
            });
        }

        public Texture2D? Texture { get; set; }

        public Vector2 Offset { get; set; }

        public Vector2 Scale { get; set; }
    }
}
