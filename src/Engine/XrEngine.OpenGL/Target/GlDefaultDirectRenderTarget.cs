#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlDefaultDirectRenderTarget : IGlRenderTarget
    {
        readonly GL _gl;

        public GlDefaultDirectRenderTarget(GL gl)
        {
            _gl = gl;
            Flags = GlRenderTargetFlags.Main;
        }

        public void Begin(Camera camera)
        {
            Debug.Assert(camera.ViewSize.Width > 0 && camera.ViewSize.Height > 0);

            GlState.Current.SetView(new Rect2I(camera.ViewSize));

            GlState.Current.BindFrameBuffer(FramebufferTarget.Framebuffer, 0);
            _gl.DrawBuffers([DrawBufferMode.Back]);

            OpenGLRender.Current!.Begin(this);
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            return null;
        }

        public void End(bool discardDepth)
        {

        }

        public void Dispose()
        {

        }

        public GlRenderTargetFlags Flags { get; set; }

        public int ShadingRate { get; set; }

        public Size2I RenderSize { get; set; }

        public Rect2I[]? ClipRegions { get; set; }
    }
}
