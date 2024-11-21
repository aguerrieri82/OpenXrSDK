using System.Runtime.InteropServices;


namespace Common.Interop
{
    public class NativeArray<TBase> : IDisposable where TBase : unmanaged
    {
        private nint _buffer;
        private readonly int _bufferSize;
        private readonly int _itemSize;

        public NativeArray(int length, Type itemType)
        {
            _itemSize = Marshal.SizeOf(itemType);
            _bufferSize = length * _itemSize;
            _buffer = MemoryManager.Allocate(_bufferSize, this);
        }

        public NativeArray(ref byte[] buffer, Type itemType)
        {
            _buffer = MemoryManager.Allocate(buffer.Length, this);
            _bufferSize = buffer.Length;
            _itemSize = Marshal.SizeOf(itemType);
        }

        public unsafe TBase* ItemPointer(int index)
        {
            return ItemPointer<TBase>(index);
        }

        public unsafe T* ItemPointer<T>(int index) where T : unmanaged
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException();

            if (sizeof(T) > _itemSize)
                throw new InvalidOperationException();

            return (T*)(_buffer + _itemSize * index);
        }

        public unsafe ref T Item<T>(int index) where T : unmanaged
        {
            var pItem = ItemPointer<T>(index);

            return ref pItem[0];
        }

        public ref TBase Item(int index)
        {
            return ref Item<TBase>(index);
        }

        public TBase[] ToArray()
        {
            var result = new TBase[Length];
            for (var i = 0; i < Length; i++)
                result[i] = Item(i);
            return result;
        }

        public void Dispose()
        {
            if (_buffer != 0)
            {
                MemoryManager.Free(_buffer);
                _buffer = 0;
            }

            GC.SuppressFinalize(this);
        }

        public unsafe TBase* Pointer => (TBase*)_buffer;

        public int Length => _bufferSize / _itemSize;
    }
}
