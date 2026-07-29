#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;
using System.Numerics;


namespace XrEngine.OpenGL
{
    public class GlCompositonPass : GlBaseRenderPass, IGlCompositor
    {
        struct CompositionImage
        {
            public GlTexture Texture;

            public Bounds2? Region;
        }

        readonly List<CompositionImage> _textures = [];

        public GlCompositonPass(OpenGLRender renderer)
            : base(renderer) 
        {

        }

        public void AppendTexture(GlTexture texture, Bounds2? region = null)
        {
            _textures.Add(new CompositionImage()
            {
                Region = region,
                Texture = texture
            });
        }

        public override void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled || _textures.Count == 0)
                return;

            var isMultiView = _renderer.RenderTarget is GlMultiViewRenderTarget;

            foreach (var item in _textures)
            {
                if (item.Region != null)
                {
                    var region = item.Region.Value;

                    _gl.Scissor((int)region.Min.X,
                                (int)region.Min.Y,
                                (uint)region.Size.X,
                                (uint)region.Size.Y);

                    _renderer.State.EnableFeature(EnableCap.ScissorTest, true);
                }
                else
                    _renderer.State.EnableFeature(EnableCap.ScissorTest, false);

                OverlayTexture(item.Texture, isMultiView);
            }

            _textures.Clear();

            _renderer.State.EnableFeature(EnableCap.ScissorTest, false);
        }
    }
}
