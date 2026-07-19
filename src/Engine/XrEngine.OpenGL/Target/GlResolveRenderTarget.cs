#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlResolveRenderTarget : IGlRenderTarget, IGlFrameBufferProvider
    {
        protected readonly GlRenderTargetPool _pool;
        protected readonly uint _sampleCount;
        protected readonly bool _isMultiView;

        protected IGlRenderTarget? _activeRt;
        protected IGlFrameBuffer? _activeFb;


        public GlResolveRenderTarget(GL gl, bool isMultiView, uint sampleCount)
        {
            _pool = new GlRenderTargetPool(gl, isMultiView)
            {
                UseIntermediateColor = true,
                IntermediateFormat = TextureFormat.RgbaFloat16
            };

            _sampleCount = sampleCount;
            _isMultiView = isMultiView;
        }


        public void Select(uint colorTex, uint depthTex)
        {
            _activeRt = _pool.GetRenderTarget(colorTex, depthTex, _sampleCount, -1, true);

            if (_activeRt is IGlFrameBufferProvider fb)
                _activeFb = fb.FrameBuffer;
        }

        public void Clear()
        {
            _activeRt = null;
            _activeFb = null;
            _pool.Clear();
        }


        public void Begin(Camera camera)
        {
            Debug.Assert(_activeRt != null);

            _activeRt.Begin(camera);
        }

        public void Dispose()
        {
            _pool.Dispose();

            GC.SuppressFinalize(this);
        }

        public void End(bool discardDepth)
        {
            Debug.Assert(_activeRt != null);

            _activeRt.End(discardDepth);

            _activeFb?.Invalidate(InvalidateFramebufferAttachment.ColorAttachment0);
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            Debug.Assert(_activeFb != null);

            if (attachment == FramebufferAttachment.ColorAttachment0)
                return _pool.IntermediateColor;

            if (attachment == FramebufferAttachment.DepthAttachment && _activeFb.Depth is GlTexture glTex)
                return glTex;

            return null;
        }

        public TextureFormat ColorFormat
        {
            get => _pool.IntermediateFormat;
            set => _pool.IntermediateFormat = value;
        }

        public IGlFrameBuffer FrameBuffer => _activeFb ?? throw new NotSupportedException();

        public bool IsMultiView => _isMultiView;

        public IShaderHandler? ShaderHandler => _activeRt?.ShaderHandler;

        public GlRenderTargetFlags Flags { get; set; }
    }
}