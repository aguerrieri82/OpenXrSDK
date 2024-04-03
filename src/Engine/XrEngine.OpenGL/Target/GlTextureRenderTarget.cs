#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace XrEngine.OpenGL
{
    public class GlTextureRenderTarget : IGlRenderTarget
    {
        protected static readonly Dictionary<uint, GlTextureRenderTarget> _targets = [];

        protected readonly GlBaseFrameBuffer _frameBuffer;
        protected readonly GL _gl;
        protected readonly uint _colorTex;
        protected readonly uint _depthTex;
        protected readonly uint _sampleCount;

        protected GlTextureRenderTarget(GL gl, uint colorTex, uint depthTex, uint sampleCount)
        {
            _gl = gl;
            _colorTex = colorTex;
            _depthTex = depthTex;
            _sampleCount = sampleCount;
            _frameBuffer = CreateFrameBuffer(colorTex, depthTex, sampleCount);
        }

        protected virtual GlBaseFrameBuffer CreateFrameBuffer(uint colorTex, uint depthTex, uint sampleCount)
        {
            return new GlTextureFrameBuffer(_gl, new GlTexture(_gl, colorTex), new GlTexture(_gl, depthTex));
        }

        public void Begin()
        {
            _frameBuffer.BindDraw();
        }

        public void End()
        {
            _frameBuffer.Unbind();
        }

        public void Dispose()
        {
            _targets.Remove(_colorTex);
            _frameBuffer.Dispose();

            GC.SuppressFinalize(this);
        }

        public uint QueryTexture(FramebufferAttachment attachment)
        {
            return _frameBuffer.QueryTexture(attachment);
        }

        public static GlTextureRenderTarget Attach(GL gl, uint colorTex, uint depthTex, uint sampleCount)
        {
            if (!_targets.TryGetValue(colorTex, out var target))
            {
                target = new GlTextureRenderTarget(gl, colorTex, depthTex, sampleCount);
                _targets[colorTex] = target;
            }

            return target;
        }

        public GlBaseFrameBuffer FrameBuffer => _frameBuffer;

    }
}
