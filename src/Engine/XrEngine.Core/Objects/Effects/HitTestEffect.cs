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

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uDrawId", DrawId);
            });
        }

        public uint DrawId { get; set; }
    }
}
