using XrMath;

namespace XrEngine
{
    public class DepthCopyMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthCopyMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "copy_depth.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                UpdateHandler = StandardVertexShaderHandler.Instance
            };
        }

        public DepthCopyMaterial()
            : base()
        {
            _shader = SHADER;
            WriteColor = true;
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
