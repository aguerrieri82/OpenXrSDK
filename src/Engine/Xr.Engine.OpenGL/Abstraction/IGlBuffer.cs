#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace Xr.Engine.OpenGL
{
    public interface IGlBuffer : IBuffer, IDisposable
    {
        void Bind();

        void Unbind();

        void AssignSlot();

        BufferTargetARB Target { get; }

        uint Handle { get; }

        uint Slot { get; }
    }
}
