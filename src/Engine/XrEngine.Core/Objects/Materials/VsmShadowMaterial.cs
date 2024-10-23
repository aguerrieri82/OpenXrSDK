namespace XrEngine
{
    public class VsmShadowMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static VsmShadowMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "vsm_shadow_map.frag",
                IsLit = false,
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
