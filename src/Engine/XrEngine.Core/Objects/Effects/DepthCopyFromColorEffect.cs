namespace XrEngine
{
    public class DepthCopyFromColorEffect : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static DepthCopyFromColorEffect()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "copy_depth_color.frag",
                VertexSourceName = "fullscreen.vert",
                Resolver = str => Embedded.GetString(str),
            };
        }

        public DepthCopyFromColorEffect()
            : base()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Opaque;
            UseDepth = false;
            WriteDepth = true;
            DepthLocation = 1;
            Channel = "r";
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature($"DEPTH_LOCATION {DepthLocation}");

            if (Texture == null)
            {
                bld.AddFeature($"USE_FETCH");
                bld.AddExtension("GL_EXT_shader_framebuffer_fetch");
            }

            bld.AddFeature($"CHANNEL {Channel}");

            if (HighPrecision)
                bld.AddFeature($"PRECISION highp");
            else
                bld.AddFeature($"PRECISION mediump");

            bld.ExecuteAction((_, up) =>
            {
                if (Texture != null)
                    up.LoadTexture(Texture, TextureSlots.ProjDepth);
            });

            base.UpdateShaderMaterial(bld);
        }

        public bool HighPrecision { get; set; }

        public int DepthLocation { get; set; }

        public Texture? Texture { get; set; }

        public string Channel { get; set; }
    }
}
