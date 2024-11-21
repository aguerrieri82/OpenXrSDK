using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Common.Interop
{
    public unsafe class ArrayMemoryBuffer<T> : IMemoryBuffer<T>, IDisposable
    {
        GCHandle _handle;
        T[] _data;
        int _lockCount;

        public ArrayMemoryBuffer(uint size)
        {
            _data = new T[size];
        }

        public ArrayMemoryBuffer(T[] data)
        {
            _data = data;
        }

        public ArrayMemoryBuffer(Span<T> data)
        {
            _data = data.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Allocate(uint size)
        {
            if (_data.Length != size)
                _data = new T[size];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* Lock()
        {
            if (_lockCount == 0)
                _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            
            _lockCount++;

            return (T*)_handle.AddrOfPinnedObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryLock<T> MemoryLock()
        {
            return new MemoryLock<T>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unlock()
        {
            _lockCount--;
            if (_lockCount == 0)
                _handle.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return _data;
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
                _handle.Free();
            GC.SuppressFinalize(this);
        }

        public uint Size => (uint)_data.Length;

        public T[] Data => _data;
    }
}
