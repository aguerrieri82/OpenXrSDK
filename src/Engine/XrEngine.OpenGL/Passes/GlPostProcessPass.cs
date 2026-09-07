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
            SourceMode = PostProcessSourceMode.Head;
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
            var isHead = (SourceMode & PostProcessSourceMode.Head) != 0;
            var isTail = (SourceMode & PostProcessSourceMode.Tail) != 0;
            var sourceIndex = isTail ? 2u : 0u;
            var destIndex = isHead ? 2u : 0u;
            var copy = (SourceMode & PostProcessSourceMode.Copy) != 0;
            var isVirtual = (SourceMode & PostProcessSourceMode.Virtual) != 0;

            Debug.Assert(isHead != isTail);

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

                if (!_views.TryGetValue(realColor.Handle, out var destView))
                {
                    if (!_effect.IsMultiView)
                        throw new NotSupportedException();

                    if (isVirtual)
                        destView = realColor.CreateVirtualView(destIndex, 2);
                    else
                        destView = realColor.CreateView(destIndex, 2);

                    _views[realColor.Handle] = destView;
                }

                _effect.Texture = color.ToEngineTexture();

                if (color.IsView)
                    _effect.BaseSourceIndex = color.ParentTexture == color ? color.ViewMinLayer : 0;
                else
                    _effect.BaseSourceIndex = sourceIndex;

                passTarget = _pool!.GetRenderTarget(destView!.Handle, 0, sourceTarget.FrameBuffer.SampleCount, createDepth: false);
            }

            passTarget?.Begin(ctx.MainCamera!);

            UseEffect(_effect);

            DrawQuad();

            passTarget?.End(false);
      
            if (realColor.Depth == 4 && copy)
                realColor.CopyTo(realColor, 0, (int)destIndex, (int)sourceIndex, 2);
        }

        public override void Dispose()
        {
            foreach (var view in _views)
            {
                if (view.Value.ParentTexture != view.Value)
                    view.Value.Dispose();
            }
            
            _views.Clear();
            
            _pool?.Dispose();
            _pool = null;

            base.Dispose();
        }

        public PostProcessSourceMode SourceMode { get; set; }

        public bool UseFxAA { get; set; }
    }
}