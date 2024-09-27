namespace XrEngine
{
    public class EyeTextureMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static EyeTextureMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "texture.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
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

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);

                if (((PerspectiveCamera)ctx.Camera!).ActiveEye == 0)
                    up.SetUniform("uTexture", LeftTexture!, 0);
                else
                    up.SetUniform("uTexture", RightTexture!, 0);
            });

        }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;

        public Texture2D? LeftTexture { get; set; }

        public Texture2D? RightTexture { get; set; }
    }
}
