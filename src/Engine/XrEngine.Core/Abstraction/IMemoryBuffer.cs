using System.Runtime.CompilerServices;

namespace XrEngine
{
    public unsafe struct MemoryLock<T> : IDisposable
    {
        private readonly IMemoryBuffer<T> _buffer;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryLock(IMemoryBuffer<T> buffer)
        {
            _buffer = buffer;
            Data = _buffer.Lock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _buffer.Unlock();
            Data = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(MemoryLock<T> obj)
        {
            return obj.Data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator nint(MemoryLock<T> obj)
        {
            return (nint)obj.Data;
        }

        public T* Data;
    }

    public unsafe interface IMemoryBuffer<T>
    {
        MemoryLock<T> MemoryLock();

        T* Lock();

        void Unlock();

        void Allocate(uint size);   

        uint Size { get; }

        Span<T> AsSpan();
    }
}
