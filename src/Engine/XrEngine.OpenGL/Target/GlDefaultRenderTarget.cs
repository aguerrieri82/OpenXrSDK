#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlDefaultRenderTarget : IGlRenderTarget, IGlFrameBufferProvider
    {
        readonly GL _gl;
        private GlTexture? _color;
        private IGlRenderAttachment? _depth;
        private bool _isResolved;
        private readonly GlTextureFrameBuffer _frameBuffer;
        private readonly uint _sampleCount;
        private readonly bool _useRenderBuffer;

        public GlDefaultRenderTarget(GL gl, bool useRenderBuffer, uint sampleCount)
        {
            _gl = gl;
            _sampleCount = sampleCount;
            _useRenderBuffer = useRenderBuffer;
            _frameBuffer = new GlTextureFrameBuffer(_gl);

            DepthFormat = TextureFormat.Depth24Stencil8;
            ColorFormat = TextureFormat.SRgba32;

            SetSize(new Size2I(16, 16));
        }

        protected void SetSize(Size2I size)
        {
            _color?.Dispose();
            _depth?.Dispose();

            _color = new GlTexture(_gl)
            {
                MaxLevel = 0,
                SampleCount = _sampleCount,
                Target = _sampleCount > 1 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D
            };

            _color.SetLabel("Deafult RT - Color");

            _color.Allocate(size.Width, size.Height, 1, ColorFormat);

            if (_useRenderBuffer)
            {
                var depthBuf = new GlRenderBuffer(_gl);

                depthBuf.Update(size.Width, size.Height, _sampleCount, DepthFormat.ToInternalFormat());

                depthBuf.SetLabel("Deafult RT - DepthBuf");

                _depth = depthBuf;
            }
            else
            {
                var depthTex = new GlTexture(_gl)
                {
                    MaxLevel = 0,
                    SampleCount = _sampleCount,
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                    Target = _sampleCount > 1 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D
                };

                depthTex.SetLabel("Deafult RT - Depth");

                depthTex.Allocate(size.Width, size.Height, 1, DepthFormat);

                _depth = depthTex;
            }

            _frameBuffer.Configure(_color, _depth, _sampleCount);
        }

        public void Begin(Camera camera)
        {
            Debug.Assert(camera.ViewSize.Width > 0 && camera.ViewSize.Height > 0);

            GlState.Current.SetView(new Rect2I(camera.ViewSize));

            if (camera.ViewSize.Width != _frameBuffer.Color!.Width || camera.ViewSize.Height != _frameBuffer.Color.Height)
                SetSize(camera.ViewSize);

            _frameBuffer.BindDraw();

            OpenGLRender.Current!.Begin(this);

            _isResolved = false;
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Resolve(bool discardDepth, GlTextureFrameBuffer src, GlTextureFrameBuffer? dst)
        {
            if (discardDepth)
                src.Invalidate(InvalidateFramebufferAttachment.DepthAttachment);

            src.BindRead(ReadBufferMode.ColorAttachment0);

            if (dst == null)
            {
                GlState.Current.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, 0);
                _gl.DrawBuffers(GlState.DRAW_BACK);
            }
            else
                dst.BindDraw();

            var w = src.Color!.Width;
            var h = src.Color.Height;

            _gl.BlitFramebuffer(0, 0, (int)w, (int)h, 0, 0, (int)w, (int)h, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            src.Unbind();

            dst?.Unbind();

            if (dst == null)
                _isResolved = true;
        }

        public void End(bool discardDepth)
        {
            if (!_isResolved)
                Resolve(discardDepth, _frameBuffer, null);
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.ColorAttachment0)
                return _color;

            if (attachment == FramebufferAttachment.DepthAttachment)
                return _frameBuffer.QueryTexture(FramebufferAttachment.DepthAttachment);

            return null;
        }

        public TextureFormat DepthFormat { get; set; }

        public TextureFormat ColorFormat { get; set; }

        IGlFrameBuffer IGlFrameBufferProvider.FrameBuffer => _frameBuffer;

        public GlTextureFrameBuffer FrameBuffer => _frameBuffer;

        public GlTexture? Color => _color;

        public GlRenderTargetFlags Flags => GlRenderTargetFlags.Main;

        public int ShadingRate { get; set; }

    }
}
