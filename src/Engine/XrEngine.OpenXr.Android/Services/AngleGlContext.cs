
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework.Angle;
using XrEngine.OpenGL;

namespace XrEngine.OpenXr.Android
{
    public class AngleGlContext : IGlContext
    {
        Thread? _ownerThread;
        IAngleContext _ctx;

        public AngleGlContext(IAngleContext ctx)
        {
            _ctx = ctx; 
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public void Release()
        {
            _ctx.ReleaseCurrent();
            if (_ownerThread == Thread.CurrentThread)
                _ownerThread = null;
        }

        public void Take()
        {
            _ownerThread = Thread.CurrentThread;
            _ctx.MakeCurrent();
        }

        public void SwapBuffers()
        {
            _ctx.SwapBuffers();
        }

        public GL Gl => _ctx.Gl!;

        public Thread? OwnerThread => _ownerThread;

        public IAngleContext AngleContext => _ctx;    
    }
}
