
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif

namespace XrEngine.OpenGL
{
    public class GlFence : IDisposable
    {
        private nint _handle;
        GL _gl;

        GlFence(GL  gl, SyncCondition condition)
        {
            _handle = gl.FenceSync(condition, SyncBehaviorFlags.None);
            _gl = gl;
        }

        public void Wait(ulong time = ulong.MaxValue)
        {
            _gl.WaitSync(_handle, SyncBehaviorFlags.None, time);
        }

        public void Dispose()
        {
            if (_handle !=0)
            {
                _gl.DeleteSync(_handle);
                _handle = 0;    
            }

            GC.SuppressFinalize(this);
        }

        public static GlFence Create(GL gl, SyncCondition condition)
        {
            return new GlFence(gl, condition);
        }
    }
}
