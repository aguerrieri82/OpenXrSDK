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
                Resolver = str => Embedded.GetString(str),
                ForcePrimitive = DrawPrimitive.Line,               
                IsLit = false,
                Priority = -1
            };
        }


        public OutlineMaterial()
            : base()
        {
            _shader = SHADER;
            UseDepth = true;
            WriteDepth = false;
            Alpha = AlphaMode.Opaque;
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
            container.ReadObject(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            //bld.AddFeature("FORCE_Z 1.0");
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uColor", Color);
            });
        }

        public float Size { get; set; }

        public Color Color { get; set; }

        float ILineMaterial.LineWidth => Size;

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;
    }
}
