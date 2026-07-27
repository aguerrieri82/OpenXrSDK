namespace XrEngine
{
    public class RayCollisionEffect : ShaderMaterial
    {
        public static readonly StandardVertexShader SHADER;

        static RayCollisionEffect()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "ray_collision.frag",
                IsLit = false
            };
        }

        public RayCollisionEffect()
            : base()
        {
            _shader = SHADER;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uDrawId", DrawId);
                up.SetUniform("uSize", Size);
            });
        }

        public uint DrawId { get; set; }

        public uint Size { get; set; }
    }
}
