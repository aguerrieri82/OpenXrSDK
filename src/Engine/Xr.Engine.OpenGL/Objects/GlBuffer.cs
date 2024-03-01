#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
#endif


namespace Xr.Engine.OpenGL
{
    public class GlBuffer<T> : GlObject, IBuffer 
    {
        private readonly BufferTargetARB _bufferType;
        private uint _length;

        public unsafe GlBuffer(GL gl, BufferTargetARB bufferType)
             : base(gl)
        {
            _gl = gl;
            _bufferType = bufferType;

            _handle = _gl.GenBuffer();

            GlDebug.Log($"GenBuffer {_handle}");
        }

        public unsafe GlBuffer(GL gl, Span<T> data, BufferTargetARB bufferType)
            : this(gl, bufferType)
        {
            Update(data);
        }

        public unsafe void Update(IntPtr data, int size)
        {
            Bind();

            var byteSpan = new ReadOnlySpan<byte>((byte*)data, size);

            _gl.BufferData(_bufferType, byteSpan, BufferUsageARB.StaticDraw);

            _length = (uint)size;

            Unbind();
        }

        public unsafe void Update(ReadOnlySpan<T> data)
        {
            Bind();
     
            fixed (T* pData = &data[0])
                Update(new nint(pData), data.Length * sizeof(T));

            _length = (uint)data.Length;

            Unbind();
        }

        public void Bind()
        {

            _gl.BindBuffer(_bufferType, _handle);
            GlDebug.Log($"BindBuffer {_bufferType} {_handle}");
        }

        public void Unbind()
        {

            _gl.BindBuffer(_bufferType, 0);
            GlDebug.Log($"BindBuffer {_bufferType} NULL");
        }

        public override void Dispose()
        {
            _gl.DeleteBuffer(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }

        public uint Length => _length;
    }
}
