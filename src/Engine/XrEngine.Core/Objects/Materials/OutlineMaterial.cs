using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class OutlineMaterial : ShaderMaterial, IColorSource
    {
        public static readonly Shader SHADER;

        static OutlineMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "outline.frag",
                VertexSourceName = "Utils/fullscreen.vert",
                Resolver = str => Embedded.GetString(str),       
                IsLit = false,
                Priority = -1
            };
        }


        public OutlineMaterial()
            : base()
        {
            _shader = SHADER;
            UseDepth = false;
            WriteDepth = false;
            Alpha = AlphaMode.Blend;
            Color = new Color(1, 1, 0, 0.7f);
            Size = 5;
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
            var depthTex = bld.Context.RenderEngine!.GetDepth()!;

            if (depthTex.SampleCount > 1)
            {
                bld.AddExtension("GL_OES_texture_storage_multisample_2d_array");
                bld.AddFeature("MULTISAMPLE");
            }
    
            bld.ExecuteAction((ctx, up) =>
            {
                var depthTex = ctx.RenderEngine!.GetDepth()!;

                up.SetUniform("uTexSize", new Vector2(1.0f / depthTex.Width, 1.0f / depthTex.Height));
                up.SetUniform("uDepth", depthTex, 10);
                up.SetUniform("uSize", Size);
                up.SetUniform("uColor", Color);
            });
        }

        public float Size { get; set; }

        public Color Color { get; set; }
    }
}
