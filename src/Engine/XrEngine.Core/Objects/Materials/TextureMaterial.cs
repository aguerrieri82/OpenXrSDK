using XrMath;

namespace XrEngine
{
    public class TextureMaterial : ShaderMaterial, IColorSource
    {
        static readonly Shader SHADER;

        static TextureMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "texture.frag",
                IsLit = false,
            };
        }
        public TextureMaterial()
            : base()
        {
            _shader = SHADER;
            Color = Color.White;
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

            if (Texture?.Transform != null)
            {
                bld.AddFeature("USE_TRANSFORM");
                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uUvTransform", Texture.Transform.Value);
                });
            }

            if (CheckTexture)
            {
                bld.AddFeature("CHECK_TEXTURE");
                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uHasTexture", Texture == null ? 0u : 1u);
                });
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                if (Texture != null)
                    up.SetUniform("uTexture", Texture, 0);

                up.SetUniform("uColor", Color);
            });
        }

        public override void Dispose()
        {
            Texture?.Dispose();
            Texture = null;
            base.Dispose();
        }

        public bool CheckTexture { get; set; }

        public Texture2D? Texture { get; set; }

        public Color Color { get; set; }
    }
}
