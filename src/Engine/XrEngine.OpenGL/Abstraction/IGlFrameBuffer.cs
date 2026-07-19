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

        void Attach(IGlRenderAttachment attachment, FramebufferAttachment slot, bool useDraw, int layer = 0);

        void Detach(FramebufferAttachment slot);

        GlTexture GetOrCreateEffect(FramebufferAttachment slot);

        void CopyTo(IGlFrameBuffer dst, ClearBufferMask mask = ClearBufferMask.ColorBufferBit);

        void Bind();

        void BindDraw();

        void BindDraw(params DrawBufferMode[] modes);

        void BindRead(ReadBufferMode mode);

        void Unbind();

        void Check(bool force = false);

        void Invalidate(params InvalidateFramebufferAttachment[] attachments);

        void BeginUpdate();

        void EndUpdate();

        Size2I Size { get; }

        uint SampleCount { get; }
    }
}
