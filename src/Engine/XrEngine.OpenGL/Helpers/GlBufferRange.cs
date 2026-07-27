#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Runtime.CompilerServices;

namespace XrEngine.OpenGL
{
    public class GlBufferRangeSlot<T> : ISimpleBuffer<T>, IDisposable
    {
        protected readonly int _index;
        protected readonly GlBufferRange<T> _range;
        protected readonly WeakReference<EngineObject> _owner;

        protected readonly GL _gl;

        private bool _isDisposed;

        internal GlBufferRangeSlot(GL gl, GlBufferRange<T> range, int index, EngineObject owner)
        {
            _owner = new WeakReference<EngineObject>(owner);
            _index = index;
            _range = range;
            _gl = gl;
        }

        public void Update(T value)
        {
            _range.Buffer.UpdateRange([value], _index);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
        }

        public void Load(GlBaseProgram program)
        {
            _range.Load();

            program.SetUniform(_range.UniformName, _index);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _range.Release(this);

            GC.SuppressFinalize(this);
        }

        void ISimpleBuffer.Update(object value)
        {
            Update((T)value);
        }

        public int Index => _index;

        public EngineObject? Owner
        {
            get
            {
                _owner.TryGetTarget(out var result);
                return result;
            }
        }

        public uint Handle => _range.Buffer.Handle;

        public long Version { get; set; }
    }

    public interface IGlBufferRange : IDisposable
    {
        void Load();

        ISimpleBuffer Reserve(EngineObject owner);

        void Release(ISimpleBuffer slot);

    }

    public class GlBufferRange<T> : IGlBufferRange
    {
        private const int AllocationChunkSize = 512;

        protected readonly GL _gl;
        protected readonly GlBuffer<T> _buffer;

        protected readonly string _uniformName;
        protected readonly int _slot;

        private readonly Dictionary<object, GlBufferRangeSlot<T>> _slotsByOwner = [];
        private readonly Stack<int> _freeSlots = new();

        private GlBufferRangeSlot<T>?[] _slots = [];
        private int _nextSlot;
        private bool _isDisposed;

        public GlBufferRange(GL gl, string uniformName, int slot)
        {
            _gl = gl;
            _buffer = new GlBuffer<T>(_gl, BufferTargetARB.ShaderStorageBuffer);
            _uniformName = uniformName;
            _slot = slot;
        }

        public void Load()
        {
            GlState.Current.LoadBuffer(_buffer, _slot);
        }

        public GlBufferRangeSlot<T> Reserve(EngineObject owner)
        {
            if (_slotsByOwner.TryGetValue(owner, out var existing))
                return existing;

            int index;

            if (_freeSlots.Count > 0)
            {
                index = _freeSlots.Pop();
            }
            else
            {
                index = _nextSlot++;

                if (index >= _slots.Length)
                {
                    var newCapacity = _slots.Length + AllocationChunkSize;

                    Array.Resize(ref _slots, newCapacity);

                    _buffer.Resize((uint)(newCapacity * Unsafe.SizeOf<T>()), preserve: true);
                }
            }

            var result = new GlBufferRangeSlot<T>(_gl, this, index, owner);

            _slots[index] = result;

            _slotsByOwner.Add(owner, result);

            owner.SetProp($"BufferSlot@{typeof(T).FullName}", result);

            return result;
        }

        public void Release(GlBufferRangeSlot<T> slot)
        {
            _slots[slot.Index] = null;

            if (slot.Owner != null)
                _slotsByOwner.Remove(slot.Owner);
            
            _freeSlots.Push(slot.Index);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            _slotsByOwner.Clear();
            _freeSlots.Clear();
            _slots = [];

            _buffer.Dispose();

            GC.SuppressFinalize(this);
        }


        ISimpleBuffer IGlBufferRange.Reserve(EngineObject owner)
        {
            return Reserve(owner);
        }

        void IGlBufferRange.Release(ISimpleBuffer slot)
        {
            Release((GlBufferRangeSlot<T>)slot);
        }

        public string UniformName => _uniformName;

        public GlBuffer<T> Buffer => _buffer;
    }
}