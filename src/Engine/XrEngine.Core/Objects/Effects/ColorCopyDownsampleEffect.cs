using System.Diagnostics;

namespace XrEngine
{
    public class ColorCopyDownsampleEffect : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static ColorCopyDownsampleEffect()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "copy_attachment_downsample.frag",
                VertexSourceName = "fullscreen.vert",
                Resolver = str => Embedded.GetString(str),
                Priority = -1
            };
        }

        public ColorCopyDownsampleEffect()
            : base()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Opaque;
            UseDepth = false;
            WriteDepth = false;
            WriteColor = false;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (IsMultiView)
            {
                bld.AddExtension("GL_OVR_multiview2");
                bld.AddFeature("MULTI_VIEW");
            }

            if (SourceTexture == null)
            {
                bld.AddFeature($"FB_MODE");
                bld.AddExtension("GL_EXT_shader_framebuffer_fetch");
            }
            else
            {
                if (SourceTexture.SampleCount > 1)
                    bld.AddFeature($"MULTISAMPLE {SourceTexture.SampleCount}");
            }

            bld.AddFeature($"DOWNSAMPLE {DownsampleFactor}");

            bld.AddFeature($"DEST_FORMAT rgba8");

            bld.ExecuteAction((ctx, up) =>
            {
                Debug.Assert(DestTexture != null);

                up.LoadImage(DestTexture, 1, BufferAccessMode.Write);

                if (SourceTexture != null)
                    up.LoadTexture(SourceTexture, 0);
            });

            base.UpdateShaderMaterial(bld);
        }

        public override bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return base.NeedUpdateShader(ctx) ||
                _tracker.IsChanged(() => DownsampleFactor) ||
                _tracker.IsChanged(() => IsMultiView);
        }

        public bool IsMultiView { get; set; }

        public Texture2D? SourceTexture { get; set; }

        public Texture2D? DestTexture { get; set; }

        public int DownsampleFactor { get; set; }
    }
}
