
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

            if (IsSrgb)
                bld.AddFeature($"SRGB");

            if (ToneMap != ToneMapMode.None)
                bld.AddFeature($"TONE_MAP {(int)ToneMap}");

            bld.ExecuteAction((ctx, up) =>
            {
                if (Texture != null)
                    up.LoadTexture(Texture, 0, true);
            });
        }

        [Notify(ChangeType.Render)]
        public partial bool IsSrgb { get; set; }

        [Notify(ChangeType.Render)]
        public partial bool IsMultiView { get; set; }

        [Notify(ChangeType.Render)]
        public partial ToneMapMode ToneMap { get; set; }

        public Texture? Texture { get; set; }  
    }
}
