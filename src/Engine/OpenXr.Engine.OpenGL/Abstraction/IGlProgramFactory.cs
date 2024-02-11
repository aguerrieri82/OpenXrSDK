using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL
{
    public interface IGlProgramFactory
    {
        GlProgram CreateProgram(GL gl, string vSource, string fSource, GlRenderOptions options);
    }
}
