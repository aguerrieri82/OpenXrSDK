#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{

    public class GlBuffer<T> : GlObject, IGlBuffer
    {
        protected readonly BufferTargetARB _target;
        protected uint _length;


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

        public unsafe void Update(IntPtr data, int size, bool wait)
        {
            Bind();

            var byteSpan = new ReadOnlySpan<byte>((byte*)data, size);

            var usage = _target == BufferTargetARB.UniformBuffer ? BufferUsageARB.StreamDraw : BufferUsageARB.StaticDraw;

            _gl.BufferData(_target, byteSpan, usage);

            _length = (uint)size;

            Unbind();
        }

        public unsafe T* Map(MapBufferAccessMask access)
        {
            var ptr = _gl.MapBufferRange(_target, 0, (nuint)(_length * sizeof(T)), access);
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
                    Update(new nint(pData), data.Length * sizeof(T), true);
            }

            _length = (uint)data.Length;
        }

        unsafe void IBuffer.Update(object value)
        {
            if (value is IDynamicBuffer dynamic)
            {
                using var dynBuffer = dynamic.GetBuffer();
                Update(dynBuffer.Data, dynBuffer.Size, false);
            }
            else
            {
                var data = (T)value;
                Update(new nint(&data), sizeof(T), true);
            }
        }

        public void Bind()
        {
            _gl.BindBuffer(_target, _handle);
        }

        public void Unbind()
        {
            _gl.BindBuffer(_target, 0);
        }


        public override void Dispose()
        {
            if (_handle != 0)
            {
                _gl.DeleteBuffer(_handle);
                _handle = 0;
            }
            GC.SuppressFinalize(this);
        }

        public unsafe void Allocate(uint length)
        {
            if (_length == length)
                return;

            _gl.BufferData(_target, (nuint)(length * sizeof(T)), null, BufferUsageARB.StreamDraw);
            _length = length; 
        }

        public string Hash { get; set; }

        public long Version { get; set; }   

        public int Slot { get; set; }   

        public BufferTargetARB Target => _target;

        public uint Length => _length;

    }
}
