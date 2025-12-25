using XrMath;

namespace XrEngine
{
    public class EyeTextureMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static EyeTextureMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "texture.frag",
                IsLit = false
            };
        }
        public EyeTextureMaterial()
            : base()
        {
            _shader = SHADER;
        }

        public EyeTextureMaterial(Texture2D left, Texture2D right)
            : this()
        {
            LeftTexture = left;
            RightTexture = right;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<EyeTextureMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<EyeTextureMaterial>(this);
        }



        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (LeftTexture?.Type == TextureType.External)
            {
                bld.AddExtension("GL_OES_EGL_image_external_essl3");
                bld.AddFeature("EXTERNAL");
            }

            bld.ExecuteAction((ctx, up) =>
            {
                if (LeftTexture == null || RightTexture == null)
                    return;

                if (((PerspectiveCamera)ctx.PassCamera!).ActiveEye == 0)
                    up.LoadTexture(LeftTexture, 0);
                else
                    up.LoadTexture(RightTexture, 0);

                up.SetUniform("uColor", Color.White);
            });
        }


        public override void Dispose()
        {
            LeftTexture?.Dispose();
            RightTexture?.Dispose();
            LeftTexture = null;
            RightTexture = null;
            base.Dispose();
        }

        public Texture2D? LeftTexture { get; set; }

        public Texture2D? RightTexture { get; set; }
    }
}
