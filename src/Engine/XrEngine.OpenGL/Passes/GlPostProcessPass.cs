#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlPostProcessPass : GlBaseRenderPass
    {
        readonly PostProcessEffect _effect;
        readonly Dictionary<uint, GlTexture> _views = [];

        GlRenderTargetPool? _pool;

        public GlPostProcessPass(OpenGLRender renderer)
            : base(renderer)
        {
            _effect = new PostProcessEffect();
        }

        public override void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled)
                return;

            var curTarget = _renderer.RenderTarget;

            if (curTarget is not IGlRenderTargetFB fbTarget)
                throw new NotSupportedException();

            var color = fbTarget.FrameBuffer.Color!;

            _effect.UseFxAA = UseFxAA;
            _effect.Texture = color.ToEngineTexture();

            IGlRenderTargetFB? renderTarget = null;

            if (curTarget is GlDefaultRenderTarget glDefaultRender)
            {
                _effect.IsMultiView = false;
                glDefaultRender.BindResolve();
            }
            else
            {
                if (!_isInit)
                {
                    bool isMultiview = fbTarget.FrameBuffer is GlMultiViewFrameBuffer;

                    _pool = new GlRenderTargetPool(_renderer.GL, isMultiview);
                    _effect.IsMultiView = isMultiview;

                    _isInit = true;
                }

                if (color.Depth == 1)
                    return;

                if (!_views.TryGetValue(color.Handle, out var viewTexture))
                {
                    viewTexture = _effect.IsMultiView ? color.CreateView(2, 2) : color.CreateView(1, 1);
                    _views[color.Handle] = viewTexture;
                }

                renderTarget = _pool!.GetRenderTarget(viewTexture!.Handle, 0, fbTarget.FrameBuffer.SampleCount);
            }

            renderTarget?.Begin(ctx.MainCamera!);

            UseEffect(_effect);

            DrawQuad();

            renderTarget?.End(false);
        }

        public bool UseFxAA { get; set; }
    }
}