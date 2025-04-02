namespace XrEngine.Helpers
{
    public class BufferList<T> : IDisposable where T : unmanaged
    {
        readonly IBuffer<T> _buffer;
        readonly Stack<int> _freeIndices = [];
        readonly int _blockSize;
        int _capacity;

        public BufferList(IBuffer<T> buffer, int blockSize = 64)
        {
            _buffer = buffer;
            _blockSize = blockSize;
        }

        public unsafe int Add(T value)
        {
            var index = _freeIndices.Count > 0 ? _freeIndices.Pop() : (int)_buffer.SizeBytes;
            if (index >= _capacity)
            {
                _capacity += _blockSize;
                _buffer.Allocate((uint)(_capacity * sizeof(T)));
            }
            return index;
        }


        public unsafe bool Remove(int index)
        {
            if (index < 0 || index >= _capacity)
                return false;

            if (_freeIndices.Contains(index))
                return false;

            _freeIndices.Push(index);

            return true;
        }

        public void Dispose()
        {
            if (_buffer is IDisposable disposable)
                disposable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
