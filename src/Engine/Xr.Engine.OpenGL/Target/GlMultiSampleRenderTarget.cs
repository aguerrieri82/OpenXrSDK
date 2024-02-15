#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace OpenXr.Engine.OpenGL
{
    public class GlMultiSampleRenderTarget : IGlRenderTarget
    {
        static readonly Dictionary<uint, GlMultiSampleRenderTarget> _targets = [];

        private readonly GlTextureFrameBuffer _destFrameBuffer;
        private readonly GlTextureFrameBuffer _renderFrameBuffer;
        private readonly GL _gl;


        GlMultiSampleRenderTarget(GL gl, uint destTexId, uint sampleCount)
        {
            _destFrameBuffer = new GlTextureFrameBuffer(gl, new GlTexture2D(gl, destTexId), false);

            var msaaTex = gl.GenTexture();

            gl.BindTexture(TextureTarget.Texture2DMultisample, msaaTex);
            gl.TexStorage2DMultisample(
                TextureTarget.Texture2DMultisample,
                sampleCount,
                (SizedInternalFormat)_destFrameBuffer.Color.InternalFormat,
                _destFrameBuffer.Color.Width,
                _destFrameBuffer.Color.Height,
                true);

            _renderFrameBuffer = new GlTextureFrameBuffer(gl,
                new GlTexture2D(gl, msaaTex, sampleCount), true, sampleCount);

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

        public static GlMultiSampleRenderTarget Attach(GL gl, uint destTexId, uint sampleCount)
        {
            if (!_targets.TryGetValue(destTexId, out var target))
            {
                target = new GlMultiSampleRenderTarget(gl, destTexId, sampleCount);
                _targets[destTexId] = target;
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
