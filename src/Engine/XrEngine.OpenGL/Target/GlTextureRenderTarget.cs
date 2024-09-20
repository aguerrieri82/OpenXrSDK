#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlTextureRenderTarget : IGlRenderTarget
    {
        protected readonly GlTextureFrameBuffer _frameBuffer;
        protected readonly GL _gl;

        public GlTextureRenderTarget(GL gl)
        {
            _gl = gl;
            _frameBuffer = new GlTextureFrameBuffer(_gl);
        }

        public GlTextureRenderTarget(GL gl, uint colorTex, uint depthTex, uint sampleCount)
            : this(gl)
        {
            _frameBuffer.Configure(colorTex, depthTex, sampleCount);
        }

        public void Begin()
        {
            _frameBuffer.Bind();
        }

        public void End()
        {
            _frameBuffer.Unbind();
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();

            GC.SuppressFinalize(this);
        }

        public uint QueryTexture(FramebufferAttachment attachment)
        {
            return _frameBuffer.QueryTexture(attachment);
        }

        public GlTextureFrameBuffer FrameBuffer => _frameBuffer;

    }
}
