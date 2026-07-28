
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
        uint _lastDepthTex;

        public GlDepthCopyPass(OpenGLRender renderer, bool multiView)
            : base(renderer)
        {
            _pool = new GlRenderTargetPool(renderer.GL, multiView);

            _effect = new DepthCopyFromColorEffect();

        }

        public override void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled)
                return;

            if (_lastDepthTex == 0)
                return;

            var renderTarget = _pool.GetRenderTarget(0, _lastDepthTex, 1);

            if (_renderer.RenderTarget is not IGlRenderTargetFB curTarget)
                throw new NotSupportedException();

            renderTarget.Begin(ctx.PassCamera!);

            var glTex = curTarget.FrameBuffer.GetOrCreateEffect(FramebufferAttachment.ColorAttachment1);

            _effect.Texture = glTex.ToEngineTexture();

            UseEffect(_effect);

            DrawQuad();

            renderTarget.End(false);

            _lastDepthTex = 0;
        }

        public void Configure(uint depthTex)
        {
            _lastDepthTex = depthTex;
        }

        public override void Dispose()
        {
            _pool.Dispose();
            base.Dispose();
        }

    }
}
