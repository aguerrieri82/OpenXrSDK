using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenGL
{
    public class GlDefaultProgramFactory : IGlProgramFactory
    {
        public virtual GlProgram CreateProgram(GL gl, ShaderMaterial material, GlRenderOptions options)
        {
            if (material is PbrMaterial)
                return new GlPbrProgram(gl, material.Shader!.Resolver!, options);

            var shader = material.Shader!;
            var resolver = shader.Resolver!;
            return new GlSimpleProgram(gl, resolver(shader.VertexSourceName!), resolver(shader.FragmentSourceName!), resolver, options);

        }
    }
}
