
namespace XrEngine
{
    public enum ToneMapMode
    {
        None = 0,
        Normal = 1,
        Neutral = 2
    }

    public partial class ResolveEffect : DynamicMaterial
    {
        private Texture? _texture;

        public ResolveEffect()
            : base("fullscreen.vert", "resolve.frag")
        {
            UseDepth = false;
            WriteDepth = false;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (IsMultiView)
            {
                bld.AddExtension("GL_OVR_multiview2");
                bld.AddFeature("MULTI_VIEW");
            }

            if (ResolveAlpha)
                bld.AddFeature($"RESOLVE_ALPHA");

            if (EncodeSrgb)
                bld.AddFeature($"SRGB");

            if (ToneMap != ToneMapMode.None)
                bld.AddFeature($"TONE_MAP {(int)ToneMap}");

            if (Texture == null)
            {
                bld.AddFeature($"FB_MODE");
                bld.AddExtension("GL_EXT_shader_framebuffer_fetch");
            }

            bld.AddFeature($"SAMPLE_COUNT {SampleCount}");

            bld.ExecuteAction((ctx, up) =>
            {
                if (Texture != null)
                    up.LoadTexture(Texture, 0, true);
            });
        }

        [Notify(ChangeType.Render)]
        public partial bool EncodeSrgb { get; set; }

        [Notify(ChangeType.Render)]
        public partial bool IsMultiView { get; set; }

        [Notify(ChangeType.Render)]
        public partial ToneMapMode ToneMap { get; set; }

        public Texture? Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                SampleCount = (_texture is Texture2D tex2 ? tex2.SampleCount : 1);
            }
        }

        [Notify(ChangeType.Render)]
        public partial bool ResolveAlpha { get; set; }


        [Notify(ChangeType.Render)]
        public partial uint SampleCount { get; set; }
    }
}
