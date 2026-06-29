#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public class GlResolvePass : GlBaseRenderPass
    {
        readonly ResolveEffect _resolve;
        readonly GlRenderPassTarget _passTarget;

        public GlResolvePass(OpenGLRender renderer)
            : base(renderer)
        {
            _resolve = new();
            _passTarget = new GlRenderPassTarget(renderer.GL);
        }

        public override void Render(RenderContext ctx)
        {
            if (!IsEnabled)
                return;

            Debug.Assert(_renderer.RenderTarget != null);

            _resolve.IsSrgb = !_renderer.UpdateContext.IsSrgb;
            _resolve.IsSrgb = false;
            _resolve.ToneMap = ToneMapMode.Neutral;
            _resolve.ResolveAlpha = false;

            //_resolve.ToneMap = _renderer.Options.ToneMap;

            if (_renderer.RenderTarget is GlDefaultRenderTarget def)
            {
                var color = def.Color!;

                if (color.SampleCount > 1)
                {
                    _passTarget.Configure(color.Width, color.Height, color.InternalFormat.GetTextureFormat());

                    def.Resolve(false, def.FrameBuffer, (GlTextureFrameBuffer)_passTarget.FrameBuffer!);

                    _passTarget.RenderTarget!.Begin(ctx.Camera!);

                    _resolve.Texture = _passTarget.Color!.ToEngineTexture();

                    UseEffect(_resolve);

                    DrawQuad();

                    def.Resolve(true, (GlTextureFrameBuffer)_passTarget.FrameBuffer!, null);

                    return;
                }
            }
            else if (_renderer.RenderTarget is GlSwapRenderTarget swap)
            {
                var color = swap.FrameBuffer.Color;

                swap.DestFrameBuffer.Bind();

                _resolve.Texture = color!.ToEngineTexture();
                _resolve.IsMultiView = swap.IsMultiView;

                UseEffect(_resolve);

                DrawQuad();

            }
            else
            {
                if (_renderer.RenderTarget is not IGlFrameBufferProvider srcTarget)
                    throw new NotSupportedException();

                var color = _renderer.RenderTarget.QueryTexture(FramebufferAttachment.ColorAttachment0);

                _passTarget.Configure(color!.Width, color.Height, color.InternalFormat.GetTextureFormat());

                srcTarget.FrameBuffer.CopyTo(_passTarget.FrameBuffer!);

                _passTarget.RenderTarget!.Begin(ctx.Camera!);

                _resolve.Texture = _passTarget.Color!.ToEngineTexture();

                _resolve.IsMultiView = _renderer.RenderTarget is GlMultiViewRenderTarget;

                UseEffect(_resolve);

                DrawQuad();

                _passTarget.FrameBuffer!.CopyTo(srcTarget.FrameBuffer);
            }
        }
    }
}