namespace Common.Interop
{
    public unsafe class NativeMemoryBuffer<T> : IMemoryBuffer<T>
    {
        readonly T* _data;
        readonly uint _size;

        public NativeMemoryBuffer(T* data, uint size)
        {
            _data = data;
            _size = size;
        }

        public void Allocate(uint size)
        {
            throw new NotSupportedException();
        }

        public T[] AsArray()
        {
            return AsSpan().ToArray();
        }

        public Span<T> AsSpan()
        {
            return new Span<T>(_data, (int)_size);
        }

        public T* Lock()
        {
            return _data;
        }

        public MemoryLock<T> MemoryLock()
        {
            return new MemoryLock<T>(this);
        }

        public void Unlock()
        {
        }

        public uint Size => _size;

    }
}
