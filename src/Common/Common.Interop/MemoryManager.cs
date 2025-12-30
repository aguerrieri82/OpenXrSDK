using System.Collections.Concurrent; // Added for thread safety
using System.Runtime.InteropServices;

namespace Common.Interop
{
    public static unsafe class MemoryManager
    {
        struct MemoryBlock
        {
            public nint Data;
            public int Size;
            public WeakReference Owner;
        }


#if DEBUG
        static readonly ConcurrentDictionary<nint, MemoryBlock> _blocks = new();
#endif

        public static nint Allocate(int size, object? owner)
        {
            var ptr = (nint)NativeMemory.AllocZeroed((nuint)size);

#if DEBUG
            var block = new MemoryBlock
            {
                Data = ptr,
                Size = size,
                Owner = new WeakReference(owner)
            };

            _blocks.TryAdd(ptr, block);
#endif
            return ptr;
        }

        public static void Free(nint data)
        {
            if (data == 0)
                return;


            NativeMemory.Free((void*)data);

#if DEBUG
            _blocks.TryRemove(data, out _);
#endif
        }
    }
}