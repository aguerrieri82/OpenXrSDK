using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL
{
    public class OpenGLRender : IRenderEngine
    {
        protected IGLContext _context;
        protected GL _gl;

        public OpenGLRender(IGLContext context, GL gl)
        {
            _context = context;
            _gl = gl;   
        }

        public void Render(Scene scene, Camera camera)
        {

        }

        public void SetImageTarget(uint image)
        {
        }
    }
}
