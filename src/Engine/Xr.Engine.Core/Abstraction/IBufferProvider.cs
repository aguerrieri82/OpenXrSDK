using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine
{
    public interface IBufferProvider
    {
        IBuffer GetBuffer<T>(string name, T data, bool isGlobal);
    }
}
