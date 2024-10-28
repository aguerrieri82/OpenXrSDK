#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using SkiaSharp;

#endif

using XrMath;


namespace XrEngine.OpenGL
{
    public class GlDefaultRenderTarget : IGlRenderTarget, IGlFrameBufferProvider
    {
        readonly GL _gl;
        private readonly GlTexture _color;
        private readonly GlRenderBuffer _depth;
        private readonly GlTextureFrameBuffer _frameBuffer;

  
        public GlDefaultRenderTarget(GL gl)
        {
            _gl = gl;

            _color = new GlTexture(_gl);
            _color.MaxLevel = 0;
            _color.IsMutable = true;    
            
            _depth = new GlRenderBuffer(_gl);
            
            SetSize(new Size2I(16, 16));    

            _frameBuffer = new GlTextureFrameBuffer(_gl,_color, _depth);
        }

        protected void SetSize(Size2I size)
        {
            var data = new TextureData
            {
                Width = size.Width,
                Height = size.Height,
                Format = TextureFormat.Rgba32
            };

            _color.Update(1, data);
            _depth.Update(size.Width, size.Height, 1, InternalFormat.Depth24Stencil8);
        }

        public void Begin(Camera camera, Size2I viewSize)
        {  
            if (viewSize.Width !=_frameBuffer.Color!.Width || viewSize.Height != _frameBuffer.Color.Height)
                SetSize(viewSize);
    
            _frameBuffer.Bind();
        }

        public void Dispose()
        {
            _frameBuffer.Dispose(); 
            GC.SuppressFinalize(this);
        }

        public void End(bool discardDepth)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _frameBuffer.Handle);
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, 0);

            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
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
                return GlDepthUtils.GetDepthUsingCopy(_gl, this);

            return null;
        }

        public IGlFrameBuffer FrameBuffer => _frameBuffer;

    }
}
