namespace XrEngine
{
    public class DepthViewMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthViewMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "depth.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                Priority = 1
            };
        }

        public DepthViewMaterial()
            : base()
        {
            _shader = SHADER;
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.SetUniform("uModel", (ctx) => ctx.Model!.WorldMatrix);

            if (Texture != null)
            {
                bld.ExecuteAction((ctx, up) =>
                {
                    if (Texture.SampleCount <= 1)
                        up.SetUniform("uTexture0", Texture, 0);
                    else
                    {
                        up.SetUniform("uTexture0MS", Texture, 0);
                        bld.AddFeature("SAMPLES " + Texture.SampleCount);
                    }
                });
            }

            if (Camera != null)
            {
                bld.SetUniform("uNearPlane", ctx => Camera.Near);
                bld.SetUniform("uFarPlane", ctx => Camera.Far);

                if (Camera is PerspectiveCamera)
                    bld.AddFeature("LINEARIZE");
            }
        }

        public Texture2D? Texture { get; set; }

        public Camera? Camera { get; set; }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;

    }
}
