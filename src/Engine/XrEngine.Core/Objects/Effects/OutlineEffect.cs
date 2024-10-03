using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class OutlineEffect : ShaderMaterial, IColorSource
    {
        public static readonly Shader SHADER;

        static OutlineEffect()
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


        public OutlineEffect()
            : base()
        {
            _shader = SHADER;
            UseDepth = false;
            WriteDepth = false;
            Alpha = AlphaMode.Blend;
            Color = new Color(1, 1, 0, 0.7f);
            Size = 5;
        }

        public OutlineEffect(Color color, float size)
            : this()
        {
            Color = color;
            Size = size;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<OutlineEffect>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            var depthTex = bld.Context.DepthMap;

            if (depthTex != null && depthTex.SampleCount > 1)
            {
                bld.AddExtension("GL_OES_texture_storage_multisample_2d_array");
                bld.AddFeature("MULTISAMPLE");
            }

            bld.ExecuteAction((ctx, up) =>
            {
                if (ctx.DepthMap == null)
                    return;
                up.SetUniform("uTexSize", new Vector2(1.0f / ctx.DepthMap.Width, 1.0f / ctx.DepthMap.Height));
                up.SetUniform("uDepth", ctx.DepthMap!, 10);
                up.SetUniform("uSize", Size);
                up.SetUniform("uColor", Color);
            });
        }

        public float Size { get; set; }

        public Color Color { get; set; }
    }
}
