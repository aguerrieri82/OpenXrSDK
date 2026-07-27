#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Xml.Linq;
#endif

namespace XrEngine.OpenGL
{
    public class GlResolvePass : GlBaseRenderPass, IToneMapper
    {
        readonly ResolveEffect _resolve;
        readonly GlRenderPassTarget _passTarget;

        public GlResolvePass(OpenGLRender renderer)
            : base(renderer)
        {
            _resolve = new();
            _passTarget = new GlRenderPassTarget(renderer.GL);
            _passTarget.Name = "Resolve";

            _resolve.ToneMap = _renderer.Options.ToneMap;

            Context.Implement<IToneMapper>(this);
        }

        public override void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled)
                return;

            _resolve.EncodeSrgb = ctx.NeedSrgbEncode;

            if (_renderer.RenderTarget is GlDefaultRenderTarget def)
            {
                var color = def.Color!;

                if (color.SampleCount > 1)
                {
                    _passTarget.ColorFormat = color.InternalFormat.ToTextureFormat();
                    _passTarget.Configure(color.Width, color.Height);

                    def.Resolve(false, def.FrameBuffer, (GlTextureFrameBuffer)_passTarget.FrameBuffer!);

                    _passTarget.RenderTarget!.Begin(ctx.PassCamera!);

                    _resolve.Texture = _passTarget.Color!.ToEngineTexture();

                    UseEffect(_resolve);

                    DrawQuad();

                    def.Resolve(true, (GlTextureFrameBuffer)_passTarget.FrameBuffer!, null);

                    return;
                }
                else
                    throw new NotSupportedException();
            }
            else if (_renderer.RenderTarget is GlResolveRenderTarget res)
            {
                res.FrameBuffer.BindDraw(DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1);

                _resolve.IsMultiView = res.IsMultiView;

                if (_resolve.Texture != null)
                {
                    _resolve.Texture = null;
                    _resolve.NotifyChanged();
                }

                UseEffect(_resolve);

                DrawQuad();

                res.FrameBuffer.BindDraw(DrawBufferMode.ColorAttachment0);
            }
            else
            {
                if (_renderer.RenderTarget is not IGlFrameBufferProvider srcTarget)
                    throw new NotSupportedException();

                var color = _renderer.RenderTarget.QueryTexture(FramebufferAttachment.ColorAttachment0);

                _passTarget.ColorFormat = color!.InternalFormat.ToTextureFormat();
                _passTarget.Configure(color.Width, color.Height);

                srcTarget.FrameBuffer.CopyTo(_passTarget.FrameBuffer!);

                _passTarget.RenderTarget!.Begin(ctx.PassCamera!);

                _resolve.Texture = _passTarget.Color!.ToEngineTexture();

                _resolve.IsMultiView = _renderer.RenderTarget is GlMultiViewRenderTarget;

                UseEffect(_resolve);

                DrawQuad();

                _passTarget.FrameBuffer!.CopyTo(srcTarget.FrameBuffer);
            }
        }

        public bool IsGlobal => _renderer.Options.ToneMap != ToneMapMode.None;

        public ToneMapMode ToneMap
        {
            get => _resolve.ToneMap;
            set => _resolve.ToneMap = value;
        }

        public bool ResolveAlpha
        {
            get => _resolve.ResolveAlpha;
            set => _resolve.ResolveAlpha = value;
        }

        public bool EncodeSrgb
        {
            get => _resolve.EncodeSrgb;
            set => _resolve.EncodeSrgb = value;
        }
    }
}