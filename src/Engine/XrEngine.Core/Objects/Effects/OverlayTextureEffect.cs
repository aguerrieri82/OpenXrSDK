namespace XrEngine
{
    public partial class OverlayTextureEffect : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static OverlayTextureEffect()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "texture_full.frag",
                VertexSourceName = "fullscreen.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                Priority = -1
            };
        }

        public OverlayTextureEffect()
        {
            Shader = SHADER;
            UseDepth = false;
            WriteDepth = false;
            Alpha = AlphaMode.Blend;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                if (Texture != null)
                    up.LoadTextureFixSrgb(ctx, Texture, TextureSlots.Albedo);
            });

        }

        [Notify(ChangeType.Render)]
        public partial Texture? Texture { get; set; }
    }
}
