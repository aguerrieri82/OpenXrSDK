#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace XrEngine.OpenGL
{
    public class GlMultiSampleRenderTarget : IGlRenderTarget
    {
        static readonly Dictionary<uint, GlMultiSampleRenderTarget> _targets = [];

        private readonly GlTextureFrameBuffer _destFrameBuffer;
        private readonly GlTextureFrameBuffer _renderFrameBuffer;
        private readonly GL _gl;


        GlMultiSampleRenderTarget(GL gl, uint destColorTex, uint destDepthTex, uint sampleCount)
        {
            _destFrameBuffer = new GlTextureFrameBuffer(gl, new GlTexture2D(gl, destColorTex), new GlTexture2D(gl, destDepthTex));

            var msColorTex = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2DMultisample, msColorTex);
            gl.TexStorage2DMultisample(
                TextureTarget.Texture2DMultisample,
                sampleCount,
                (SizedInternalFormat)_destFrameBuffer.Color.InternalFormat,
                _destFrameBuffer.Color.Width,
                _destFrameBuffer.Color.Height,
                true);

            var msDepthTex = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2DMultisample, msDepthTex);
            gl.TexStorage2DMultisample(
                TextureTarget.Texture2DMultisample,
                sampleCount,
                (SizedInternalFormat)_destFrameBuffer.Depth!.InternalFormat,
                _destFrameBuffer.Depth!.Width,
                _destFrameBuffer.Depth!.Height,
                true);

            _renderFrameBuffer = new GlTextureFrameBuffer(gl,
                new GlTexture2D(gl, msColorTex, sampleCount), 
                new GlTexture2D(gl, msDepthTex, sampleCount));

            _gl = gl;
        }

        public void Begin()
        {
            _gl.Enable(EnableCap.Multisample);
            _renderFrameBuffer.BindDraw();
        }

        public void End()
        {
            _renderFrameBuffer.CopyTo(_destFrameBuffer);
            _renderFrameBuffer.Unbind();
        }

        public static GlMultiSampleRenderTarget Attach(GL gl, uint destColorTex, uint destDepthTex, uint sampleCount)
        {
            if (!_targets.TryGetValue(destColorTex, out var target))
            {
                target = new GlMultiSampleRenderTarget(gl, destColorTex, destDepthTex, sampleCount);
                _targets[destColorTex] = target;
            }

            return target;
        }

        public void Dispose()
        {
            _targets.Remove(_destFrameBuffer.Color.Handle);

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
