using XrMath;

namespace XrEngine
{
    public class DepthOnlyMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthOnlyMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "empty.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                UpdateHandler = StandardVertexShaderHandler.Instance
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
                up.SetUniform("uColor", Color.Transparent);
            });
        }
    }
}
