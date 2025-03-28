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

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, bld) =>
            {
                bld.SetUniform("uModel", ctx.Model!.WorldMatrix);
            });
        }
    }
}
