using XrMath;

namespace XrEngine
{
    public class OutlineMaterial : ShaderMaterial, IColorSource, ILineMaterial
    {
        public static readonly Shader SHADER;

        static OutlineMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "color.frag",
                VertexSourceName = "standard.vert",
                GeometrySourceName = "outline.geom",
                Resolver = str => Embedded.GetString(str),
                Priority = 1,
                IsLit = false,
                UpdateHandler = StandardVertexShaderHandler.Instance
            };
        }


        public OutlineMaterial()
            : base()
        {
            _shader = SHADER;
        }

        public OutlineMaterial(Color color, float size)
            : this()
        {
            Color = color;
            Size = size;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<OutlineMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<OutlineMaterial>(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("PASS_VIEW");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uColor", Color);
                up.SetUniform("uSize", Size);
                up.SetUniform("uThreshold", Threshold);

            });
        }

        public Color Color { get; set; }

        [Range(1, 10f, 1)]
        public float Size { get; set; }

        [Range(0, 5, 0.01f)]
        public float Threshold { get; set; }

        float ILineMaterial.LineWidth => Size;

    }
}
