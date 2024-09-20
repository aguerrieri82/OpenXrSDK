#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace XrEngine.OpenGL
{
    [Obsolete]
    public class GlMultiSampleRenderTarget : IGlRenderTarget
    {
        private readonly GlTextureFrameBuffer _destFrameBuffer;
        private readonly GlTextureFrameBuffer _renderFrameBuffer;
        private readonly GL _gl;

        public GlMultiSampleRenderTarget(GL gl, uint destColorTex, uint destDepthTex, uint sampleCount)
        {
            _destFrameBuffer = new GlTextureFrameBuffer(gl, destColorTex, destDepthTex);

            var msColorTex = new GlTexture(gl);
            GlState.Current!.BindTexture(TextureTarget.Texture2DMultisample, msColorTex);
            gl.TexStorage2DMultisample(
                TextureTarget.Texture2DMultisample,
                sampleCount,
                (SizedInternalFormat)_destFrameBuffer.Color!.InternalFormat,
                _destFrameBuffer.Color.Width,
                _destFrameBuffer.Color.Height,
                true);

            var msDepthTex = gl.GenTexture();
            GlState.Current!.BindTexture(TextureTarget.Texture2DMultisample, msDepthTex);
            gl.TexStorage2DMultisample(
                TextureTarget.Texture2DMultisample,
                sampleCount,
                (SizedInternalFormat)_destFrameBuffer.Depth!.InternalFormat,
                _destFrameBuffer.Depth!.Width,
                _destFrameBuffer.Depth!.Height,
                true);

            _renderFrameBuffer = new GlTextureFrameBuffer(gl,
                GlTexture.Attach(gl, msColorTex, sampleCount),
                GlTexture.Attach(gl, msDepthTex, sampleCount));

            _gl = gl;
        }

        public void Begin()
        {
            _renderFrameBuffer.Bind();
        }

        public void End()
        {
            _renderFrameBuffer.CopyTo(_destFrameBuffer);
            _renderFrameBuffer.Unbind();
        }


        public void Dispose()
        {
            _destFrameBuffer.Dispose();
            _renderFrameBuffer.Dispose();

            GC.SuppressFinalize(this);
        }

        public uint QueryTexture(FramebufferAttachment attachment)
        {
            return _renderFrameBuffer.QueryTexture(attachment);
        }
    }
}
