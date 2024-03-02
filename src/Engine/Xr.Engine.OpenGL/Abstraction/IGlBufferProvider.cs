using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenGL
{
    public interface IGlBufferProvider
    {
        GlBuffer<T> GetBuffer<T>(string name, bool isGlobal);
    }
}
