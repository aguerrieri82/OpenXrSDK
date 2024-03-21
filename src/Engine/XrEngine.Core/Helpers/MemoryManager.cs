using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public static class MemoryManager
    {
        struct MemoryBlock
        {
            public nint Data;
            public int Size;
            public object? Owner;
        }

        static List<MemoryBlock> _blocks = [];
        
        public static nint Allocate(int size, object? owner)
        {
            var result = Marshal.AllocHGlobal(size);    
            GC.AddMemoryPressure(size);
            _blocks.Add(new MemoryBlock { Data = result, Size = size, Owner =  owner });
            return result;
        }

        public static void Free(nint data)
        {
            Marshal.FreeHGlobal(data);
            var idx = _blocks.FindIndex(x => x.Data == data);
            GC.RemoveMemoryPressure(_blocks[idx].Size);
            _blocks.RemoveAt(idx);  
        }
    }
}
