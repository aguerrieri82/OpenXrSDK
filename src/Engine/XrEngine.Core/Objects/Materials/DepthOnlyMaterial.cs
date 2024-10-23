namespace XrEngine
{
    public class DepthOnlyMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthOnlyMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "empty.frag",
                IsLit = false
            };
        }

        public DepthOnlyMaterial()
            : base()
        {
            _shader = SHADER;
            WriteColor = false;
            WriteDepth = true;
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
            });
        }
    }
}
