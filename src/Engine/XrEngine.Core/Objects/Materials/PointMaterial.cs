namespace XrEngine
{
    public class PointMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static PointMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "point.frag",
                VertexSourceName = "point.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }

        public PointMaterial()
            : base()
        {
            _shader = SHADER;
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, bld) =>
            {
                bld.SetUniform("uViewProj", ctx.PassCamera!.ViewProjection);
                bld.SetUniform("uModel", ctx.Model!.WorldMatrix);
            });
        }
    }
}
