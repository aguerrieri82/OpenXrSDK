#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
