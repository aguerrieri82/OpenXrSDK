namespace XrEngine
{
    public class DepthOnlyMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthOnlyMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "color.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
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
            bld.SetUniform("uColor", ctx => Color.Transparent);
            StandardVertexShaderHandler.Instance.UpdateShader(bld);
        }

        public static readonly DepthOnlyMaterial Instance = new DepthOnlyMaterial();
    }
}
