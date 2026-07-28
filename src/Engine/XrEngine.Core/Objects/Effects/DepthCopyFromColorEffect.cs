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
        }


        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature($"DEPTH_LOCATION {DepthLocation}");

            bld.ExecuteAction((_, up) =>
            {
                if (Texture != null)
                    up.LoadTexture(Texture, TextureSlots.ProjDepth);
            });

            base.UpdateShaderMaterial(bld);
        }

        public int DepthLocation { get; set; }

        public Texture? Texture { get; set; }
    }
}
