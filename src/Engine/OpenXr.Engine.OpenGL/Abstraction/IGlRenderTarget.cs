using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL
{
    public interface IGlRenderTarget : IDisposable
    {
        void Begin();

        void End();
    }
}
