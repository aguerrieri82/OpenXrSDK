namespace XrEngine
{
    public class DepthViewMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthViewMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "depth_view.frag",
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
            bld.SetUniform("uNormalMatrix", (ctx) => ctx.Model!.NormalMatrix);

            if (Texture != null)
            {
                bld.ExecuteAction((ctx, up) =>
                {
                    bld.AddFeature("SAMPLES " + Texture.SampleCount);
                    up.SetUniform("uTexture", Texture, 0);
                });
            }

            if (Camera is PerspectiveCamera)
            {
                bld.AddFeature("LINEARIZE");
                bld.SetUniform("uNearPlane", ctx => Camera.Near);
                bld.SetUniform("uFarPlane", ctx => Camera.Far);
            }
        }

        public Texture2D? Texture { get; set; }

        public Camera? Camera { get; set; }

    }
}
