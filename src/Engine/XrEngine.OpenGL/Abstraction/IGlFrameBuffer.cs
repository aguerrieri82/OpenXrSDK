#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public interface IGlFrameBuffer : IGlObject
    {
        GlTexture? Color { get; }

        IGlRenderAttachment? Depth { get; }

        GlTexture GetOrCreateEffect(FramebufferAttachment slot);

        void Bind();

        void Unbind();

        void Check();

        void SetDrawBuffers(params DrawBufferMode[] modes);
    }
}
