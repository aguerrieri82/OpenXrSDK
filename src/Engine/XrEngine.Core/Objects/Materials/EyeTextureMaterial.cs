namespace XrEngine
{
    public class EyeTextureMaterial : ShaderMaterial
    {
        public static readonly Shader SHADER;

        public enum CameraEye
        {
            None = -1,
            Left = 0,
            Right = 1
        }

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
            DebugEye = CameraEye.None;
        }

        public EyeTextureMaterial(Texture2D left, Texture2D right)
            : this()
        {
            LeftTexture = left;
            RightTexture = right;
            DebugEye = CameraEye.None;
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

            bld.PrepareTexture(LeftTexture);
            bld.PrepareTexture(RightTexture);

            bld.ExecuteAction((ctx, up) =>
            {
                if (FixedEye != 1 && LeftTexture != null)
                    up.LoadTextureFixSrgb(ctx, LeftTexture, 0);

                if (FixedEye != 0 && RightTexture != null)
                    up.LoadTextureFixSrgb(ctx, RightTexture, 1);

                if (DebugEye != CameraEye.None)
                    up.SetUniform("uActiveEye", (uint)DebugEye);
                else
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

        public CameraEye DebugEye { get; set; }
    }
}
