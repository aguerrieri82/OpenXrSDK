using XrMath;

namespace XrEngine
{
    public class TextureMaterial : ShaderMaterial, IColorSource
    {
        static readonly Shader SHADER;
        protected TextureSampler? _sampler;
        protected long _lastTexVersion;

        static TextureMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "texture.frag",
                UseMotionVectors = true
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

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
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
                    if (Texture?.Transform != null)
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

            bld.PrepareTexture(Texture);

            bld.ExecuteAction((ctx, up) =>
            {
                if (Texture != null)
                    up.LoadTextureFixSrgb(ctx, Texture, TextureSlots.Albedo);

                up.SetUniform("uColor", Color);
            });
        }

        protected override void OnChanged(ObjectChange change)
        {
            _lastTexVersion = -1;
            base.OnChanged(change);
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
