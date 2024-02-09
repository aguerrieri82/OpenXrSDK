using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);
        }


        public void Unbind()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }


        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
