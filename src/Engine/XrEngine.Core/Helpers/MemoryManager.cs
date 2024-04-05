using System.Runtime.InteropServices;

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

        static readonly List<MemoryBlock> _blocks = [];

        public static nint Allocate(int size, object? owner)
        {
            var result = Marshal.AllocHGlobal(size);
            GC.AddMemoryPressure(size);
            _blocks.Add(new MemoryBlock { Data = result, Size = size, Owner = owner });
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
