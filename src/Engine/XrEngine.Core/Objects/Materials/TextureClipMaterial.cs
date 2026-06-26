using XrEngine.Objects;
using XrMath;

namespace XrEngine
{
    public partial class TextureClipMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static TextureClipMaterial()
        {
            SHADER = new Shader
            {
                Resolver = str => Embedded.GetString(str),
                VertexSourceName = "clip.vert",
                FragmentSourceName = "texture_stereo.frag",
                IsLit = false,
            };
        }

        public TextureClipMaterial()
        {
            Shader = SHADER;
            UseDepth = true;
            WriteDepth = true;
            Color = Color.White;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (!IsStereo)
                bld.AddFeature("FIXED_EYE 0");

            bld.AddFeature("USE_COLOR");

            if (IsStereo)
            {
                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uActiveEye", (uint)ctx.PassCamera!.ActiveEye);

                    if (RightTexture != null)
                        up.LoadTexture(RightTexture, 1);
                });
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uColor", Color);
                
                if (MainLeftTexture != null)
                    up.LoadTexture(MainLeftTexture, 0);

            });
        }

        public Texture2D? MainLeftTexture { get; set; }

        public Texture2D? RightTexture { get; set; }

        [Notify(ChangeType.Material)]
        public partial bool IsStereo { get; set; }

        public Color Color { get; set; }

    }
}
