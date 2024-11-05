#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{

    public class GlBuffer<T> : GlObject, IGlBuffer, IBuffer<T>
    {
        protected readonly BufferTargetARB _target;
        protected uint _arrayLength;


        public unsafe GlBuffer(GL gl, BufferTargetARB target)
             : base(gl)
        {
            _target = target;
            Hash = string.Empty;
            Version = -1;
            Slot = 0;
            Create();
        }

        public unsafe GlBuffer(GL gl, Span<T> data, BufferTargetARB target)
            : this(gl, target)
        {
            Update(data);
        }

        protected void Create()
        {
            _handle = _gl.GenBuffer();
        }

        public unsafe void Update(nint data, uint sizeBytes, bool wait)
        {
            Bind();

            var newArrayLen = sizeBytes / (uint)sizeof(T); 

            if (_arrayLength != newArrayLen || _target == BufferTargetARB.UniformBuffer)
            {
                var usage = _target == BufferTargetARB.UniformBuffer ? BufferUsageARB.StreamDraw : BufferUsageARB.StaticDraw;
                _gl.BufferData(_target, sizeBytes, (void*)data, usage);
            }
            else
            {
                var pDst = Map(MapBufferAccessMask.WriteBit);
                EngineNativeLib.CopyMemory(data, (nint)pDst, sizeBytes);  
                Unmap();    
            }

            _arrayLength = newArrayLen;

            Unbind();
        }

        public unsafe T* Map(MapBufferAccessMask access)
        {
            var ptr = _gl.MapBufferRange(_target, 0, (nuint)(_arrayLength * sizeof(T)), access);
            if (ptr == null)
                throw new InvalidOperationException("MapBufferRange return NULL");
            return (T*)ptr;
        }

        public void Unmap()
        {
            _gl.UnmapBuffer(_target);
        }

        public unsafe void Update(ReadOnlySpan<T> data)
        {
            if (data.Length > 0)
            {
                fixed (T* pData = &data[0])
                    Update((nint)pData, (uint)(data.Length * sizeof(T)), true);
            }

            _arrayLength = (uint)data.Length;
        }

        public unsafe void Update(T value)
        {
            if (value is IDynamicBuffer dynamic)
            {
                using var dynBuffer = dynamic.GetBuffer();
                Update(dynBuffer.Data, dynBuffer.Size, false);
            }
            else
            {
                Update((nint)(&value), (uint)sizeof(T), true);
            }
        }

        unsafe void IBuffer.Update(object value)
        {
            Update((T)value);
        }

        public void Bind()
        {
            GlState.Current!.BindBuffer(_target, _handle);
        }

        public void Unbind()
        {
            GlState.Current!.BindBuffer(_target, 0);
        }


        public override void Dispose()
        {
            if (_handle != 0)
            {
                GlState.Current!.BindBuffer(_target, 0);
                _gl.DeleteBuffer(_handle);
                _handle = 0;
            }
            GC.SuppressFinalize(this);
        }

        public unsafe void Allocate(uint length)
        {
            if (_arrayLength == length)
                return;

            _gl.BufferData(_target, (nuint)(length * sizeof(T)), null, BufferUsageARB.StreamDraw);
            _arrayLength = length; 
        }

        public string Hash { get; set; }

        public long Version { get; set; }   

        public int Slot { get; set; }   

        public BufferTargetARB Target => _target;

        public uint ArrayLength => _arrayLength;

    }
}
