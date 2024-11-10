using XrMath;

namespace XrEngine
{
    public class HitTestEffect : ShaderMaterial
    {
        public static readonly StandardVertexShader SHADER;

        static HitTestEffect()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "hit_test.frag",
                IsLit = false
            };
        }


        public HitTestEffect()
            : base()
        {
            _shader = SHADER;
        }


        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uColor", UIntToRGBA(DrawId));
            });
        }

        static Color UIntToRGBA(uint color)
        {
            float a = ((color >> 24) & 0xFF) / 255f;
            float b = ((color >> 16) & 0xFF) / 255f;
            float g = ((color >> 8) & 0xFF) / 255f;
            float r = (color & 0xFF) / 255f;
            return new Color(r, g, b, a);
        }


        public uint DrawId { get; set; }
    }
}
