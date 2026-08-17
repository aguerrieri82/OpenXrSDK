namespace XrEngine
{
    public class HitTestEffect : ShaderMaterial
    {
        protected ChangeTracker _tracker = new();

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
            _isSkinDynamic = true;
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
