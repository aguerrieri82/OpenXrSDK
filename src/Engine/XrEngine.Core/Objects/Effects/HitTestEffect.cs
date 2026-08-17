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
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uDrawId", DrawId);
            });
        }

        public override bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return _tracker.IsChanged(() => HasSkin);
        }

        public uint DrawId { get; set; }
    }
}
