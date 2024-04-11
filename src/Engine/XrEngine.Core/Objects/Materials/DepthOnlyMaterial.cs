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
            bld.SetUniform("uModel", (ctx) => ctx.Model!.WorldMatrix);
            bld.SetUniform("uColor", ctx => Color.Transparent);
        }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;

        public static readonly DepthOnlyMaterial Instance = new();
    }
}
