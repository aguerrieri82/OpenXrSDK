
namespace XrEngine
{

    public partial class PostProcessEffect : DynamicMaterial
    {
        private Texture? _texture;

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

            bld.ExecuteAction((ctx, up) =>
            {
                if (Texture != null)
                    up.LoadTexture(Texture, 0, true);
            });
        }


        public bool IsMultiView { get; set; }

        public Texture? Texture { get; set; }

        public bool UseFxAA { get; set; }
    }
}
