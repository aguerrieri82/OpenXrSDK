
using XrMath;

namespace XrEngine
{
    public class OutlineEffect : DynamicMaterial
    {
        public OutlineEffect()
            : base("fullscreen.vert", "outline.frag")
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

            bld.AddFeature($"COLOR_LOCATON {ColorLocation}");
            bld.AddFeature($"MASK_LOCATON {MaskLocation}");
            bld.AddFeature($"OUTLINE_SIZE {OutlineSize}");

            if (Texture == null)
            {
                bld.AddFeature($"FB_MODE");
                bld.AddExtension("GL_EXT_shader_framebuffer_fetch");
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uColor", Color);

                if (Texture != null)
                    up.LoadTexture(Texture, 0);
            });
        }

        public uint ColorLocation { get; set; }

        public uint MaskLocation { get; set; }

        public bool IsMultiView { get; set; }

        public float OutlineSize { get; set; }

        public Color Color { get; set; }

        public Texture? Texture { get; set; }

        public bool UseFbNonCoherent { get; set; }
    }
}
