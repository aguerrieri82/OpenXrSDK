namespace XrEngine
{
    public class LineMaterial : ShaderMaterial, ILineMaterial
    {
        static readonly Shader SHADER;

        static LineMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "line.frag",
                VertexSourceName = "line.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }

        public LineMaterial()
            : base()
        {
            _shader = SHADER;
            LineWidth = 1;
        }


        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (bld.Context.Model != null)
                bld.SetUniform("uModel", (ctx) => ctx.Model!.WorldMatrix);
        }

        public float LineWidth { get; set; }
    }
}
