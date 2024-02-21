namespace OpenXr.Engine
{
    public class LineMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static LineMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "line.frag",
                VertexSourceName = "line.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }

        public LineMaterial()
            : base()
        {
            _shader = SHADER;
        }

        public override void UpdateShader(UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl)
        {
            if (ctx.Camera != null)
            {
                up.SetUniform("uView", ctx.Camera.Transform.Matrix);
                up.SetUniform("uProjection", ctx.Camera.Projection);
            }
            
            if (ctx.Model != null)
                up.SetUniform("uModel", ctx.Model.WorldMatrix);
        }
    }
}
