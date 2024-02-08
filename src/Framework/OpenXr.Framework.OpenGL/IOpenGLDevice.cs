using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.OpenGL
{
    public interface IOpenGLDevice : IDisposable
    {
        void Initialize(ulong minVer, ulong maxVer);

        IXrThread MainThread { get; }

        IView View { get; }

        GL Gl { get; }
    }
}
