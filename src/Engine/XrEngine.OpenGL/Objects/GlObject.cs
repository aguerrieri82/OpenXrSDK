#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public abstract class GlObject : IDisposable
    {
        protected uint _handle;
        protected GL _gl;

        protected GlObject(GL gl)
        {
            _gl = gl;
            EnableDebug = true;
        }

        public virtual void Dispose()
        {
            if (_handle != 0)
            {
                ObjectBinder.Unbind(this);
                _handle = 0;
            }

            GC.SuppressFinalize(this);
        }


        public static implicit operator uint(GlObject obj)
        {
            return obj._handle;
        }

        public uint Handle => _handle;

        public GL GL => _gl;

        public bool EnableDebug { get; set; }

        public object? Source { get; set; }
    }
}
