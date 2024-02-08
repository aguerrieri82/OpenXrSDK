using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8500

namespace OpenXr.Framework
{
    public  class NativeArray<TBase> : IDisposable where TBase : struct
    {
        private nint _buffer;
        private int _bufferSize;
        private int _itemSize;

        public NativeArray(int length, Type itemType)
        {
            _itemSize = Marshal.SizeOf(itemType);
            _bufferSize = length * _itemSize;
            _buffer = Marshal.AllocHGlobal(_bufferSize);
        }

        public NativeArray(ref byte[] buffer, Type itemType)
        {
            _buffer = Marshal.AllocHGlobal(buffer.Length);
            _bufferSize = buffer.Length;
            _itemSize = Marshal.SizeOf(itemType);   
        }

        public unsafe ref T Item<T>(int index) where T : struct
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException();

            if (sizeof(T) > _itemSize)
                throw new InvalidOperationException();

            var pItem = (T*)(_buffer + _itemSize * index);

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
            if (_buffer!= 0)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = 0;    
            }
        }

        public unsafe TBase* Pointer => (TBase*)_buffer;

        public int Length => _bufferSize / _itemSize;
    }
}
