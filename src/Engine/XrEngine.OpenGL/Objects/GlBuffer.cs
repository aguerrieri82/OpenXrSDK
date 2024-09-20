#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
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
            Hash = "";
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

            //TODO fence cause multisample multiview to not render
            /*
            if (wait && false)
            {
                var fence = _gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
                
                _gl.WaitSync(fence, SyncBehaviorFlags.None, unchecked((ulong)-1));
                
                _gl.DeleteSync(fence);  
            }
            */

            _length = (uint)size;

            Unbind();
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

        public string Hash { get; set; }

        public BufferTargetARB Target => _target;

        public uint Length => _length;

    }
}
