#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;

namespace XrEngine.OpenGL
{

    public class GlDepthCopyPass : GlBaseRenderPass
    {
        readonly GlRenderTargetPool _pool;
        readonly DepthCopyFromColorEffect _effect;
        private IGlRenderTargetFB? _renderTarget;
        private readonly bool _imageMode;
        private readonly bool _fetchSupported;

        public GlDepthCopyPass(OpenGLRender renderer, bool multiView, bool imageMode)
            : base(renderer)
        {
            _pool = new GlRenderTargetPool(renderer.GL, multiView)
            {
                ColorFormat = TextureFormat.GrayFloat32,
                Name = "Depth Copy"
            };

            _effect = new DepthCopyFromColorEffect();

            _imageMode = imageMode;

            _fetchSupported = _gl.IsExtensionPresent("EXT_shader_framebuffer_fetch");

            _flags = GlRenderPassFlags.CustomCamera;

        }

        public override void Render(GlUpdateContext ctx)
        {
            if (!IsEnabled || _renderTarget == null)
                return;

            _renderTarget.Begin(ctx.PassCamera!);

            if (_imageMode)
            {
                if (_fetchSupported)
                    _effect.Texture = null;
                else
                    _effect.Texture = ctx.CopyDepthImage;
            }
            else
            {
                if (_renderer.RenderTarget is not IGlRenderTargetFB curTarget)
                    throw new NotSupportedException();

                var glTex = curTarget.FrameBuffer.GetOrCreateEffect(FramebufferAttachment.ColorAttachment1);

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
                var ctx = _renderer.UpdateContext;

                var glDepth = GlTexture.Attach(_gl, depthTex);

                Texture2D? motionTex = null;

                uint colorTex = 0;

                if (ctx.MotionVectorProvider != null)
                {
                    motionTex = ctx.MotionVectorProvider.Texture;

                    Debug.Assert(motionTex != null);

                    if (motionTex.Width == glDepth.Width && motionTex.Height == glDepth.Height)
                    {
                        colorTex = motionTex.ToGlTexture().Handle;

                        motionTex.Tag = 1;

                        _effect.Channel = "b";
                        _effect.HighPrecision = true;
                    }
                }

                _renderTarget = _pool.GetRenderTarget(colorTex, depthTex, 1, createColor: colorTex == 0);

                if (colorTex == 0)
                {
                    _renderTarget.FrameBuffer.BindDraw();
                    _renderer.State.SetWriteColor(true);
                    _renderer.GL.ClearBuffer(BufferKind.Color, 0, [0f]);
                }
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
