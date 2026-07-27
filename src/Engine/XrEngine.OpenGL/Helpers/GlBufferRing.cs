#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public unsafe class GlBufferRing<T> : IDisposable
    {
        protected readonly GL _gl;
        protected readonly GlBuffer<T> _buffer;
        private GlFence?[] _fences = [];
        protected int _readSlot;
        protected int _writeSlot;
        protected T* _bufferData;
        private uint _slotSizeElements;
        private uint _slotCount;
        private int _updateCount;
        private T[]? _clearData;

        public GlBufferRing(GL gl, BufferTargetARB target)
        {
            _gl = gl;
            _buffer = new GlBuffer<T>(gl, target);
        }

        public void Allocate(uint slotSizeElements, uint count)
        {
            _slotSizeElements = slotSizeElements;
            _slotCount = count;

            _buffer.Allocate(slotSizeElements * (uint)sizeof(T) * count, BufferAllocateFlags.Persistent);
            _bufferData = _buffer.MapPermanentRead();

            _fences = new GlFence?[count];

            _readSlot = -1;
            _writeSlot = 0;
            _clearData = new T[_slotSizeElements];
        }

        public bool WaitRead()
        {
            return WaitRead(TimeSpan.Zero);
        }

        public bool WaitRead(TimeSpan maxTime)
        {
            if (_readSlot == -1)
                return false;

            var fence = _fences[_readSlot];

            if (fence == null)
                return false;

            var result = fence.WaitClient(maxTime);
            
            fence.Dispose();

            _fences[_readSlot] = null;

            return result;
        }

        public void Swap()
        {
            _fences[_writeSlot]?.Dispose();
            _fences[_writeSlot] = GlFence.Create(_gl);

            _readSlot = _writeSlot;
            _writeSlot = (int)((_writeSlot + 1) % _slotCount);
        }

        public void ClearWrite()
        {
            _buffer.UpdateRange(_clearData, (int)(_writeSlot * _slotSizeElements), true);
        }

        public void BindWrite(int bindSlot)
        {
            GlState.Current.LoadBufferRange(_buffer, bindSlot, 
                (int)ActiveWriteOffsetBytes, 
                (uint)(_slotSizeElements * sizeof(T)));
        }

        public int ActiveWriteSlot => _writeSlot;

        public int ActiveReadSlot => _readSlot;

        public uint ActiveReadOffsetBytes => _readSlot == -1 ?
            throw new InvalidOperationException() :
            (uint)(_readSlot * _slotSizeElements * sizeof(T));

        public uint ActiveWriteOffsetBytes => (uint)(_writeSlot * _slotSizeElements * sizeof(T));

        public ReadOnlySpan<T> ActiveReadSpan => _readSlot == -1 ?
            throw new InvalidOperationException() :
            new(_bufferData + _readSlot * _slotSizeElements, (int)_slotSizeElements);

        public Span<T> ActiveWriteSpan
            => new(_bufferData + _writeSlot * _slotSizeElements, (int)_slotSizeElements);

        public void Dispose()
        {
            foreach (var fence in _fences)
                fence?.Dispose();

            _fences = [];
            _bufferData = null;
            _buffer.Dispose();

            GC.SuppressFinalize(this);
        }

        public void BeginUpdate()
        {
            if (_updateCount == 0)
                _buffer.Bind();
            _updateCount++;
        }

        public void EndUpdate()
        {
            _updateCount--;
            if (_updateCount == 0)
            {
                _buffer.Unbind();
                Swap();
            }
        }
    }
}