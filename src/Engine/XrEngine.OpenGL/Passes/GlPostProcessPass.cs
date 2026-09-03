#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;

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
            if (!IsEnabled || (!UseFxAA))
                return;

            var curTarget = _renderer.RenderTarget;

            if (curTarget is not IGlRenderTargetFB sourceTarget)
                throw new NotSupportedException();

            var color = sourceTarget.FrameBuffer.Color!;

            var realColor = color.IsView ? color.ParentTexture! : color;

            _effect.UseFxAA = UseFxAA;
 
            IGlRenderTargetFB? passTarget = null;

            if (curTarget is GlDefaultRenderTarget glDefaultRender)
            {
                _effect.IsMultiView = false;
                _effect.Texture = color.ToEngineTexture();
                glDefaultRender.BindResolve();
            }
            else
            {
                if (!_isInit)
                {
                    bool isMultiview = sourceTarget.FrameBuffer is GlMultiViewFrameBuffer;

                    _pool = new GlRenderTargetPool(_renderer.GL, isMultiview);
                    _pool.Name = "Post Process";

                    _effect.IsMultiView = isMultiview;

                    _isInit = true;
                }

                Debug.Assert(color.Depth > 1);

                if (!_views.TryGetValue(realColor.Handle, out var viewTexture))
                {
                    viewTexture = _effect.IsMultiView ? realColor.CreateView(2, 2) : color.CreateView(1, 1);
                    _views[realColor.Handle] = viewTexture;
                }

                _effect.Texture = color.ToEngineTexture();
                _effect.BaseSourceIndex = color.IsView && color.ParentTexture == color ? color.ViewMinLayer : 0;

                passTarget = _pool!.GetRenderTarget(viewTexture!.Handle, 0, sourceTarget.FrameBuffer.SampleCount, createDepth: false);
            }

            passTarget?.Begin(ctx.MainCamera!);

            UseEffect(_effect);

            DrawQuad();

            passTarget?.End(false);
      
            if (color.Depth == 4)
                realColor.CopyTo(realColor, 0, 2, 0, 2);
        }

        public override void Dispose()
        {
            foreach (var view in _views)
                view.Value.Dispose();
            
            _views.Clear();
            
            _pool?.Dispose();
            _pool = null;

            base.Dispose();
        }

        public bool UseFxAA { get; set; }
    }
}