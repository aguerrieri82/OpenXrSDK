using XrMath;

namespace XrEngine
{
    public class VsmShadowMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static VsmShadowMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "vsm_shadow_map.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                UpdateHandler = StandardVertexShaderHandler.Instance
            };
        }

        public VsmShadowMaterial()
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
