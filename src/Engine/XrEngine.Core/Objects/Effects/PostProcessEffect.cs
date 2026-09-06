
namespace XrEngine
{

    public partial class PostProcessEffect : DynamicMaterial
    {

        public PostProcessEffect()
            : base("fullscreen.vert", "post_process.frag")
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

            if (UseFxAA)
                bld.AddFeature($"USE_FXAA");

            bld.AddFeature($"BASE_INDEX {BaseSourceIndex}u");

            bld.ExecuteAction((ctx, up) =>
            {
                if (Texture != null)
                    up.LoadTexture(Texture, 0, true);
            });
        }

        [Notify(ChangeType.Render)]
        public bool IsMultiView { get; set; }

        public Texture? Texture { get; set; }

        [Notify(ChangeType.Render)]
        public bool UseFxAA { get; set; }

        [Notify(ChangeType.Render)]
        public uint BaseSourceIndex { get; set; }
    }
}
