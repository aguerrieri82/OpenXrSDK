#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;

namespace XrEngine.OpenGL
{
    public interface IGlFrameBuffer : IGlObject
    {
        GlTexture? Color { get; }

        IGlRenderAttachment? Depth { get; }

        void BindAttachment(IGlRenderAttachment attachment, FramebufferAttachment slot, bool useDraw);

        GlTexture GetOrCreateEffect(FramebufferAttachment slot);

        void Bind();

        void Unbind();

        void Check();

        void SetDrawBuffers(params DrawBufferMode[] modes);

        Size2I Size { get; }
    }
}
