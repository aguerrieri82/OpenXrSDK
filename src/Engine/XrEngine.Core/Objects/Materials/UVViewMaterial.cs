namespace XrEngine.Materials
{
    public class UVViewMaterial : ShaderMaterial
    {
        static readonly Shader SHADER = new StandardVertexShader
        {
            FragmentSourceName = "uv_view.frag",
            IsLit = false
        };

        public UVViewMaterial()
        {
            Shader = SHADER;
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
            });
        }
    }
}
