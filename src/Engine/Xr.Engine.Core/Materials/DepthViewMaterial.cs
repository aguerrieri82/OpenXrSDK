namespace OpenXr.Engine
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
            var depth = bld.Context.RenderEngine?.GetDepth();

            if (depth != null)
            {
                if (depth.SampleCount <= 1)
                    bld.SetUniform("uTexture0", ctx => depth, 0);
                else
                    bld.SetUniform("uTexture0MS", ctx => depth, 0);

                bld.SetUniform("uSamples", ctx => depth.SampleCount);
            }

            if (bld.Context.Camera != null)
            {
                bld.SetUniform("uNearPlane", ctx => ctx.Camera!.Near);
                bld.SetUniform("uFarPlane", ctx => ctx.Camera!.Far);
            }

            bld.UpdateStandardVS();
        }

    }
}
