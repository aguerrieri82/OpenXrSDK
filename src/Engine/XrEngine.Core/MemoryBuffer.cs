using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public unsafe struct MemoryLock<T> : IDisposable
    {
        private readonly ArrayMemoryBuffer<T> _buffer;
        private T* _data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryLock(ArrayMemoryBuffer<T> buffer)
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
        public static implicit operator T* (MemoryLock<T> obj)
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
        T* Lock();

        MemoryLock<T> MemoryLock();

        void Unlock();

        void Allocate(uint size);   

        uint Size { get; }

        Span<T> AsSpan();
    }

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
