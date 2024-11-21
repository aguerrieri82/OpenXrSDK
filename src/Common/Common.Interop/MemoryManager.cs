using System.Runtime.InteropServices;

namespace Common.Interop
{
    public static class MemoryManager
    {
        struct MemoryBlock
        {
            public nint Data;
            public int Size;
            public WeakReference Owner;
        }

        static readonly Dictionary<nint, MemoryBlock> _blocks = [];

        public static nint Allocate(int size, object? owner)
        {
            var result = Marshal.AllocHGlobal(size);
            GC.AddMemoryPressure(size);
#if DEBUG
            _blocks[result] = new MemoryBlock { Data = result, Size = size, Owner = new WeakReference(owner) };
#endif
            return result;
        }

        public static void Free(nint data)
        {
            Marshal.FreeHGlobal(data);
#if DEBUG
            var memBlock = _blocks[data];
            GC.RemoveMemoryPressure(memBlock.Size);
            _blocks.Remove(data);
#endif
        }
    }
}
