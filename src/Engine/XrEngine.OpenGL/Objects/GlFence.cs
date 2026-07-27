
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

        public void WaitGpu()
        {
            _gl.WaitSync(_handle, SyncBehaviorFlags.None, 0);
        }

        public bool WaitClient(TimeSpan maxTime)
        {
            return WaitClient((ulong)(maxTime.Ticks * TimeSpan.NanosecondsPerTick));
        }

        public bool WaitClient(ulong time = ulong.MaxValue, SyncObjectMask mask = 0)
        {
            var result = _gl.ClientWaitSync(_handle, mask, time);

            return result == GLEnum.AlreadySignaled || result == GLEnum.ConditionSatisfied;
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

        public static GlFence Create(GL gl)
        {
            return new GlFence(gl, SyncCondition.SyncGpuCommandsComplete);
        }
    }
}
