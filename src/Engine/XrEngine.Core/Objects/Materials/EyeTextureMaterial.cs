namespace XrEngine
{
    public class EyeTextureMaterial : ShaderMaterial
    {
        public static readonly Shader SHADER;

        

        static EyeTextureMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "texture_stereo.frag",
                IsLit = false
            };
        }
        public EyeTextureMaterial()
            : base()
        {
            _shader = SHADER;
            FixedEye = -1;
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

            if (FixedEye != -1)
                bld.AddFeature($"FIXED_EYE {FixedEye}");

            bld.ExecuteAction((ctx, up) =>
            {
                if (LeftTexture == null || RightTexture == null)
                    return;

                up.LoadTexture(LeftTexture, 0);

                up.LoadTexture(RightTexture, 1);

                up.SetUniform("uActiveEye", (uint)((PerspectiveCamera)ctx.PassCamera!).ActiveEye);

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

        public int FixedEye { get; set; }   
    }
}
