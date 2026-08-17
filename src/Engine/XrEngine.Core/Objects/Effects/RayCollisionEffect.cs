namespace XrEngine
{
    public class RayCollisionEffect : ShaderMaterial
    {
        public static readonly StandardShader SHADER;

        static RayCollisionEffect()
        {
            SHADER = new StandardShader
            {
                FragmentSourceName = "ray_collision.frag",
                IsLit = false
            };
        }

        public RayCollisionEffect()
            : base()
        {
            _shader = SHADER;
            WriteColor = false;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uDrawId", DrawId);
                up.SetUniform("uSize", Size);
                up.SetUniform("uFrame", Frame);
            });
        }

        public uint DrawId { get; set; }

        public uint Size { get; set; }

        public uint Frame { get; set; }
    }
}
