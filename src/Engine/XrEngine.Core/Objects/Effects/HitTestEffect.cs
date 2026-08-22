namespace XrEngine
{
    public class HitTestEffect : ShaderMaterial
    {
        public static readonly StandardShader SHADER;

        static HitTestEffect()
        {
            SHADER = new StandardShader
            {
                FragmentSourceName = "hit_test.frag",
                IsLit = false
            };
        }

        public HitTestEffect()
            : base()
        {
            _shader = SHADER;
            Skin = SkinMode.Dynamic;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uDrawId", DrawId);
            });

            base.UpdateShaderModel(bld);
        }

        public uint DrawId { get; set; }
    }
}
