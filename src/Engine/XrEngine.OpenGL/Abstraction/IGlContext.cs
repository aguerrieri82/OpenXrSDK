#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenGL
{
    public interface IGlContext : IDisposable
    {
        GL Gl { get; }

        void Take();

        void Release();

        Thread? OwnerThread { get; }
    }
}
