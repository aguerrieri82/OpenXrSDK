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

        public override void UpdateShader(UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl)
        { 
            var depth = ctx.RenderEngine?.GetDepth();
            if (depth != null)
            {
                if (depth.SampleCount <= 1)
                    up.SetUniform("uTexture0", depth, 0);
                else
                    up.SetUniform("uTexture0MS", depth, 0);

                up.SetUniform("uSamples", depth.SampleCount);
            }

            if (ctx.Camera != null)
            {
                up.SetUniform("uNearPlane", ctx.Camera.Near);
                up.SetUniform("uFarPlane", ctx.Camera.Far);
            }
        }
    }
}
