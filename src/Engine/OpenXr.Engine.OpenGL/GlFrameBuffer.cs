#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace OpenXr.Engine.OpenGL
{
    public class GlFrameBuffer : GlObject
    {
        public GlFrameBuffer(GL gl)
            : base(gl)
        {

        }

        public virtual void Bind()
        {
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _handle);
        }


        public void Unbind()
        {
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }



        public override void Dispose()
        {
            _gl.DeleteFramebuffer(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }
    }
}
