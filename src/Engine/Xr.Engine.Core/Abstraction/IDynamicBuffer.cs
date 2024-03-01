using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine
{
    public unsafe struct DynamicBuffer : IDisposable
    {
        public nint Data;

        public int Size;

        public Action? Free;

        public void Dispose()
        {
            Free?.Invoke();
            Data = 0;
        }
    }

    public interface IDynamicBuffer
    {
        DynamicBuffer GetBuffer();
    }
}
