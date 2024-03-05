#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System;
#endif


namespace Xr.Engine.OpenGL
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

            _handle = _gl.GenBuffer();

            GlDebug.Log($"GenBuffer {_handle}");
        }

        public unsafe GlBuffer(GL gl, Span<T> data, BufferTargetARB target)
            : this(gl, target)
        {
            Update(data);
        }

        public unsafe void Update(IntPtr data, int size)
        {
            Bind();

            var byteSpan = new ReadOnlySpan<byte>((byte*)data, size);

            _gl.BufferData(_target, byteSpan, BufferUsageARB.StaticDraw);

            _length = (uint)size;

            Unbind();
        }

        public unsafe void Update(ReadOnlySpan<T> data)
        {
            fixed (T* pData = &data[0])
                Update(new nint(pData), data.Length * sizeof(T));

            _length = (uint)data.Length;
        }

        unsafe void IBuffer.Update(object value)
        {
            if (value is IDynamicBuffer dynamic)
            {
                using var dynBuffer = dynamic.GetBuffer();
                Update(dynBuffer.Data, dynBuffer.Size);
            }
            else
            {
                var data = (T)value;
                Update(new nint(&data), sizeof(T));
            }
        }

        public void Bind()
        {
            _gl.BindBuffer(_target, _handle);

            GlDebug.Log($"BindBuffer {_target} {_handle}");
        }

        public void Unbind()
        {
            _gl.BindBuffer(_target, 0);

            GlDebug.Log($"BindBuffer {_target} NULL");
        }


        public void AssignSlot()
        {
            GlBufferConst.UsedSlots++;
            _slot = GlBufferConst.UsedSlots;
            _gl.BindBufferBase(_target, _slot, _handle);
        }

        public override void Dispose()
        {
            _gl.DeleteBuffer(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }


        public BufferTargetARB Target => _target;

        public uint Length => _length;

        public uint Slot => _slot;
    }
}
