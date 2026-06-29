#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlSwapRenderTarget : IGlRenderTarget, IGlFrameBufferProvider
    {
        readonly GL _gl;
        readonly GlFrameBufferPool _pool;
        readonly uint _sampleCount;
        readonly bool _isMultiView;

        IGlRenderTarget? _mainRt;
        IGlFrameBuffer? _mainFb;
        IGlRenderTarget? _destRt;
        IGlFrameBuffer? _destFb;


        public GlSwapRenderTarget(GL gl, bool isMultiView, uint sampleCount)
        {
            _gl = gl;
            _pool = new GlFrameBufferPool(_gl, isMultiView);
            _sampleCount = sampleCount;
            _isMultiView = isMultiView;

            ColorFormat = TextureFormat.RgbaFloat16;
        }


        public void Select(uint colorTex, uint depthTex)
        {
            _destRt = _pool.GetRenderTarget(colorTex, depthTex, 1, false);

            if (_destRt is IGlFrameBufferProvider fb)
                _destFb = fb.FrameBuffer;

            if (_mainFb == null)
            {
                _mainRt = _pool.CreateRenderTarget(_destFb!.Color!.Width, _destFb.Color.Height, ColorFormat, _sampleCount);

                if (_mainRt is IGlFrameBufferProvider fb2)
                    _mainFb = fb2.FrameBuffer;
            }
        }

        public void Clear()
        {
            _mainFb = null;
            _mainRt = null;
            _destRt = null;
            _destFb = null;
            _pool.Clear();
        }


        public void Begin(Camera camera)
        {
            Debug.Assert(_mainRt != null && _destRt != null);

            _destRt.Begin(camera);
            _mainRt.Begin(camera);
        }

        public void CommitDepth()
        {

        }

        public void Dispose()
        {
            _pool.Dispose();
        }

        public void End(bool discardDepth)
        {
            Debug.Assert(_destRt != null && _mainRt != null);

            _mainRt.End(discardDepth);

            _destRt.End(discardDepth);
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            Debug.Assert(_mainFb != null);

            if (attachment == FramebufferAttachment.ColorAttachment0)
                return _mainFb.Color;
            
            if (attachment == FramebufferAttachment.DepthAttachment && _mainFb.Depth is GlTexture glTex)
                return glTex;
            
            return null;
        }

        public TextureFormat ColorFormat { get; set; }

        public IGlFrameBuffer FrameBuffer => _mainFb ?? throw new NotSupportedException();

        public IGlFrameBuffer DestFrameBuffer => _destFb ?? throw new NotSupportedException();

        public bool IsMultiView => _isMultiView;

        public IShaderHandler? ShaderHandler => _mainRt?.ShaderHandler;
    }
}
