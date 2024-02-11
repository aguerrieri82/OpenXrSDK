#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenXr.Engine.OpenGL
{ 
    public abstract class GlObject : IDisposable
    {
        protected uint _handle;
        protected GL _gl;

        protected GlObject(GL gl)
        {
            _gl = gl;
        }

        public abstract void Dispose();

        public uint Handle => _handle;


        public static implicit operator uint(GlObject obj)
        {
            return obj._handle;
        }
    }
}
