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
        private readonly GlTextureFrameBuffer _frameBuffer;
        private readonly uint _sampleCount;
        private readonly bool _useRenderBuffer;


        public GlDefaultRenderTarget(GL gl, bool useRenderBuffer, uint sampleCount)
        {
            _gl = gl;
            _sampleCount = sampleCount;
            _useRenderBuffer = useRenderBuffer;

            if (useRenderBuffer)
                _depth = new GlRenderBuffer(_gl);
            else
                _depth = new GlTexture(_gl)
                {
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                    MaxLevel = 0
                };

            _frameBuffer = new GlTextureFrameBuffer(_gl);

            SetSize(new Size2I(16, 16));
        }

        protected void SetSize(Size2I size)
        {
            var isTexChanged = false;

            if (_sampleCount > 1 || _color == null)
            {
                _color?.Dispose();

                _color = new GlTexture(_gl)
                {
                    MaxLevel = 0,
                    SampleCount = _sampleCount,
                    IsMutable = _sampleCount <= 1,
                    Target = _sampleCount > 1 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D
                };

                isTexChanged = true;
            }

            _color.Update(new TextureData
            {
                Width = size.Width,
                Height = size.Height,
                Format = TextureFormat.Rgba32,
            });

            if (_useRenderBuffer && _depth == null)
                _depth = new GlRenderBuffer(_gl);

            else if (!_useRenderBuffer && (_depth is not GlTexture depthTx || !depthTx.IsMutable))
            {
                _depth?.Dispose();

                _depth = new GlTexture(_gl)
                {
                    IsMutable = _sampleCount <= 1,
                    MaxLevel = 0,
                    SampleCount = _sampleCount,
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                    Target = _sampleCount > 1 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D
                };

                isTexChanged = true;
            }

            if (_depth is GlRenderBuffer renderBuffer)
                renderBuffer.Update(size.Width, size.Height, _sampleCount, InternalFormat.Depth24Stencil8);

            else if (_depth is GlTexture glTex)
                glTex.Update(new TextureData
                {
                    Width = size.Width,
                    Height = size.Height,
                    Format = TextureFormat.Depth24Stencil8,
                });

            if (isTexChanged)
                _frameBuffer.Configure(_color, _depth, _sampleCount);

        }

        public void Begin(Camera camera)
        {
            Debug.Assert(camera.ViewSize.Width > 0 && camera.ViewSize.Height > 0);

            GlState.Current!.SetView(new Rect2I(camera.ViewSize));

            if (camera.ViewSize.Width != _frameBuffer.Color!.Width || camera.ViewSize.Height != _frameBuffer.Color.Height)
                SetSize(camera.ViewSize);

            _frameBuffer.Bind();
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        public void End(bool discardDepth)
        {
            if (discardDepth)
                _frameBuffer.Invalidate(InvalidateFramebufferAttachment.DepthAttachment);

            _frameBuffer.BindRead(ReadBufferMode.ColorAttachment0);

            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, 0);

            _gl.DrawBuffers(GlState.DRAW_BACK);

            var w = _frameBuffer.Color!.Width;
            var h = _frameBuffer.Color.Height;

            _gl.BlitFramebuffer(0, 0, (int)w, (int)h, 0, 0, (int)w, (int)h, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            _frameBuffer.Unbind();
        }

        public void CommitDepth()
        {

        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.DepthAttachment)
                return _frameBuffer.QueryTexture(FramebufferAttachment.DepthAttachment);

            return null;
        }

        public IGlFrameBuffer FrameBuffer => _frameBuffer;

    }
}
