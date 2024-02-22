#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenGL
{
    public class GlDefaultProgramFactory : IGlProgramFactory
    {
        public virtual GlProgram CreateProgram(GL gl, ShaderMaterial material)
        {
            var shader = material.Shader!;
            var resolver = shader.Resolver!;
            return new GlSimpleProgram(gl, resolver(shader.VertexSourceName!), resolver(shader.FragmentSourceName!), resolver);
        }
    }
}
