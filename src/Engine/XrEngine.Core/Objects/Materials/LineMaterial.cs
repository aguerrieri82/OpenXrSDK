namespace XrEngine
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

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (bld.Context.Camera != null)
            {
                bld.SetUniform("uView", (ctx) => ctx.Camera!.View);
                bld.SetUniform("uProjection", (ctx) => ctx.Camera!.Projection);
            }

            if (bld.Context.Model != null)
                bld.SetUniform("uModel", (ctx) => ctx.Model!.WorldMatrix);
        }
    }
}
