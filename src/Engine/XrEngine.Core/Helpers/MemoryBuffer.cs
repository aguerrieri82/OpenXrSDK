using System.Runtime.CompilerServices;

namespace XrEngine
{
 
    public static class MemoryBuffer
    {
        public static IMemoryBuffer<T> CreateOrResize<T>(IMemoryBuffer<T>? buffer, uint size)
        {
            if (buffer == null)
                return Create<T>(size);
            if (buffer.Size != size)
                buffer.Allocate(size);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMemoryBuffer<T> Create<T>(uint size)
        {
            return new ArrayMemoryBuffer<T>(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMemoryBuffer<T> Create<T>(T[] array)
        {
            return new ArrayMemoryBuffer<T>(array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMemoryBuffer<T> Create<T>(Span<T> data)
        {
            return new ArrayMemoryBuffer<T>(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static IMemoryBuffer<T> Create<T>(void* pData, uint size)
        {
            return Create(new Span<T>(pData, (int)size));
        }
    }
}
