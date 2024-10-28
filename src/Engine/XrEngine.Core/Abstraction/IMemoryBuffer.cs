using System.Runtime.CompilerServices;

namespace XrEngine
{
    public unsafe struct MemoryLock<T> : IDisposable
    {
        private readonly IMemoryBuffer<T> _buffer;
        private T* _data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryLock(IMemoryBuffer<T> buffer)
        {
            _buffer = buffer;
            _data = _buffer.Lock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _buffer.Unlock();
            _data = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(MemoryLock<T> obj)
        {
            return obj._data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator nint(MemoryLock<T> obj)
        {
            return (nint)obj._data;
        }
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
