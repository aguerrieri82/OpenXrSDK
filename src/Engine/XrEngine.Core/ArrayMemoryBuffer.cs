using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XrEngine
{
    public unsafe class ArrayMemoryBuffer<T> : IMemoryBuffer<T>
    {
        GCHandle _handle;
        T[] _data;
     
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
            _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
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
            _handle.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return _data;
        }

        public uint Size => (uint)_data.Length; 

        public T[] Data => _data;   
    }
}
