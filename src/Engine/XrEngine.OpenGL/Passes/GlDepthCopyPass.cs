
#if GLES
using Android.Media.Effect;
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{

    public class GlDepthCopyPass : GlBaseRenderPass
    {
        readonly GlRenderTargetPool _pool;
        readonly DepthCopyFromColorEffect _effect;
        private IGlRenderTargetFB? _renderTarget;
        private bool _imageMode;

        public GlDepthCopyPass(OpenGLRender renderer, bool multiView, bool imageMode)
            : base(renderer)
        {
            _pool = new GlRenderTargetPool(renderer.GL, multiView)
            {
                ColorFormat = TextureFormat.GrayFloat32
            };

            _effect = new DepthCopyFromColorEffect();

            _imageMode = imageMode;

        }

        public override void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled || _renderTarget == null)
                return;

            _renderTarget.Begin(ctx.PassCamera!);

            if (_imageMode)
            {
                _effect.Texture = null;
            }
            else
            {
                var glTex = _renderTarget.FrameBuffer.GetOrCreateEffect(FramebufferAttachment.ColorAttachment1);
                _effect.Texture = glTex.ToEngineTexture();
            }

            UseEffect(_effect);

            DrawQuad();

            _renderTarget.End(false);

            _renderTarget = null;
        }

        public GlTexture? Configure(uint depthTex)
        {
            if (_imageMode)
            {
                _renderTarget = _pool.GetRenderTarget(0, depthTex, 1, createColor: true);
                _renderTarget.FrameBuffer.BindDraw();
                _renderer.State.SetWriteColor(true);
                _renderer.GL.ClearBuffer(BufferKind.Color, 0, [1f]);
            }
            else
                _renderTarget = _pool.GetRenderTarget(0, depthTex, 1);

            return _renderTarget.FrameBuffer.Color;
        }

        public override void Dispose()
        {
            _pool.Dispose();
            base.Dispose();
        }

    }
}
