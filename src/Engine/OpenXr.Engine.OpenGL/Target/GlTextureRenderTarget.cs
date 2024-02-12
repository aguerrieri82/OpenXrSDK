#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace OpenXr.Engine.OpenGL
{
    public class GlTextureRenderTarget : IGlRenderTarget
    {
        protected static readonly Dictionary<uint, GlTextureRenderTarget> _targets = [];

        protected readonly GlFrameBuffer _frameBuffer;
        protected readonly GL _gl;
        protected readonly uint _texId;
        protected readonly uint _sampleCount;

        protected GlTextureRenderTarget(GL gl, uint texId, uint sampleCount = 1)
        {
            _gl = gl;
            _texId = texId;
            _sampleCount = sampleCount;
            _frameBuffer = CreateFrameBuffer(texId, sampleCount);
        }

        protected virtual GlFrameBuffer CreateFrameBuffer(uint texId, uint sampleCount)
        {
            if (sampleCount > 1)
                throw new NotSupportedException();

            return new GlTextureFrameBuffer(_gl, new GlTexture2D(_gl, texId));
        }

        public void Begin()
        {
            if (_sampleCount == 1)
                _gl.Disable(EnableCap.Multisample);
            else
                _gl.Enable(EnableCap.Multisample);

            _frameBuffer.BindDraw();
        }

        public void End()
        {
            _frameBuffer.Unbind();
        }

        public static GlTextureRenderTarget Attach(GL gl, uint texId)
        {
            if (!_targets.TryGetValue(texId, out var target))
            {
                target = new GlTextureRenderTarget(gl, texId);
                _targets[texId] = target;
            }

            return target;
        }

        public void Dispose()
        {
            _targets.Remove(_texId);
            _frameBuffer.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
