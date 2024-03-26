#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{

    internal static class GlBufferConst
    {
        public static uint UsedSlots = 0;
    }

    public class GlBuffer<T> : GlObject, IGlBuffer
    {
        protected readonly BufferTargetARB _target;
        protected uint _length;
        protected uint _slot;

        public unsafe GlBuffer(GL gl, BufferTargetARB target)
             : base(gl)
        {
            _gl = gl;
            _target = target;
            Version = -1;
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

            if (wait)
            {
                var fence = _gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
                
                _gl.WaitSync(fence, SyncBehaviorFlags.None, unchecked((ulong)-1));
                
                _gl.DeleteSync(fence);  

                //_gl.Finish();
            }

            _length = (uint)size;

            Unbind();
        }

        public unsafe void Update(ReadOnlySpan<T> data)
        {
            fixed (T* pData = &data[0])
                Update(new nint(pData), data.Length * sizeof(T), true);

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

        public void AssignSlot()
        {
            GlBufferConst.UsedSlots++;
            _slot = GlBufferConst.UsedSlots;
            _gl.BindBufferBase(_target, _slot, _handle);
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

        public long Version { get; set; }

        public BufferTargetARB Target => _target;

        public uint Length => _length;

        public uint Slot => _slot;
    }
}
