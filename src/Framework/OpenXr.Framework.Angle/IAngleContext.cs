#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenXr.Framework.Angle
{
    public interface IAngleContext : IDisposable
    {
        void ReleaseCurrent();

        void MakeCurrent();

        void SwapBuffers();

        GL Gl { get; }

        nint Context { get; }

        nint Surface { get; }
    }
}
