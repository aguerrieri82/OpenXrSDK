#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace OpenXr.Engine.OpenGLES
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
            throw new NotImplementedException();
        }
    }
}
