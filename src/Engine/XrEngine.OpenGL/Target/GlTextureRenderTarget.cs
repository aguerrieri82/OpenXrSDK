﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlTextureRenderTarget : IGlRenderTarget, IGlFrameBufferProvider
    {
        protected readonly GlTextureFrameBuffer _frameBuffer;
        protected readonly GL _gl;

        public GlTextureRenderTarget(GL gl)
        {
            _gl = gl;
            _frameBuffer = new GlTextureFrameBuffer(_gl);
        }

        public void Begin()
        {
            _frameBuffer.Bind();
        }

        public void End(bool finalPass)
        {
            if (finalPass && _frameBuffer.Depth != null)
            {
                _frameBuffer.Bind();
                var attach = new InvalidateFramebufferAttachment[] { InvalidateFramebufferAttachment.DepthStencilAttachment };
                _gl.InvalidateFramebuffer(_frameBuffer.Target, attach.AsSpan());
            }
            _frameBuffer.Unbind();
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();

            GC.SuppressFinalize(this);
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            return _frameBuffer.QueryTexture(attachment);
        }

        public void CommitDepth()
        {
            _gl.Flush();
        }

        public GlTextureFrameBuffer FrameBuffer => _frameBuffer;

        IGlFrameBuffer IGlFrameBufferProvider.FrameBuffer => _frameBuffer;

    }
}
