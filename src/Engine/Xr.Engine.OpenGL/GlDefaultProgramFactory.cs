#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace Xr.Engine.OpenGL
{
    public class GlDefaultProgramFactory : IGlProgramFactory
    {
        public virtual GlProgram CreateProgram(GL gl, ShaderMaterial material)
        {
            var shader = material.Shader!;
            var resolver = shader.Resolver!;

            if (material is PbrMaterial pbr)
                return new GlPbrProgram(gl, resolver(shader.VertexSourceName!), resolver(shader.FragmentSourceName!), resolver);

            return new GlSimpleProgram(gl, resolver(shader.VertexSourceName!), resolver(shader.FragmentSourceName!), resolver);
        }
    }
}
