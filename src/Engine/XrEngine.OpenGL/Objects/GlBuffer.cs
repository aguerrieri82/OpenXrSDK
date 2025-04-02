#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif


namespace XrEngine.OpenGL
{

    public static class GlBuffer
    {
        public static IBuffer Create(GL gl, BufferTargetARB target, Type contentType)
        {
            var type = typeof(GlBuffer<>).MakeGenericType(contentType);
            return (IBuffer)Activator.CreateInstance(type, gl, target)!;
        }
    }

    public class GlBuffer<T> : GlObject, IGlBuffer, IBuffer<T>
    {
        protected readonly BufferTargetARB _target;
        protected uint _sizeBytes;
        protected BufferUsageARB _usage;
        protected int _updateCount;

        public unsafe GlBuffer(GL gl, BufferTargetARB target)
             : base(gl)
        {
            _target = target;
            Hash = string.Empty;
            Version = -1;
            Slot = 0;
            Create();
        }

        public unsafe GlBuffer(GL gl, ReadOnlySpan<T> data, BufferTargetARB target)
            : this(gl, target)
        {
            UpdateRange(data);
        }

        protected void Create()
        {
            _handle = _gl.GenBuffer();

            _usage = _target switch
            {
                BufferTargetARB.UniformBuffer => BufferUsageARB.StreamDraw,
                BufferTargetARB.ShaderStorageBuffer => BufferUsageARB.DynamicDraw,
                _ => BufferUsageARB.StaticDraw
            };
        }

        public unsafe void Update(void* data, uint sizeBytes, bool wait)
        {
            BeginUpdate();

            if (_sizeBytes != sizeBytes || _target == BufferTargetARB.UniformBuffer)
            {
                _gl.BufferData(_target, sizeBytes, data, _usage);
                _sizeBytes = sizeBytes;
            }
            else
            {
                var pDst = Map(MapBufferAccessMask.WriteBit);
                EngineNativeLib.CopyMemory((nint)data, (nint)pDst, sizeBytes);
                Unmap();
            }

            EndUpdate();
        }

        public void BeginUpdate()
        {
            if (_updateCount == 0)
                Bind();
            _updateCount++;
        }

        public void EndUpdate()
        {
            _updateCount--;
            if (_updateCount == 0)
                Unbind();
        }

        public unsafe void UpdateRange(void* data, uint sizeBytes, int offsetBytes, bool wait)
        {
            BeginUpdate();

            if (offsetBytes == 0 && sizeBytes >= _sizeBytes)
                _gl.BufferData(_target, sizeBytes, data, _usage);
            else
                _gl.BufferSubData(_target, offsetBytes, sizeBytes, data);

            _sizeBytes = sizeBytes + (uint)offsetBytes;

            EndUpdate();
        }

        unsafe byte* IBuffer.Lock(BufferAccessMode mode)
        {
            var mask = mode switch
            {
                BufferAccessMode.Read => MapBufferAccessMask.ReadBit,
                BufferAccessMode.Write => MapBufferAccessMask.WriteBit,
                BufferAccessMode.Replace => MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit,
                BufferAccessMode.ReadWrite => MapBufferAccessMask.ReadBit | MapBufferAccessMask.WriteBit,
                _ => throw new NotSupportedException()
            };

            return (byte*)Map(mask);
        }

        void IBuffer.Unlock()
        {
            Unmap();
        }

        public unsafe void Allocate(uint sizeInByte)
        {
            Bind();

            _gl.BufferData(_target, sizeInByte, null, _usage);

            _sizeBytes = sizeInByte;
        }

        public unsafe T* Map(MapBufferAccessMask access)
        {
            BeginUpdate();

            var ptr = _gl.MapBufferRange(_target, 0, _sizeBytes, access);
            if (ptr == null)
                throw new InvalidOperationException("MapBufferRange return NULL");

            return (T*)ptr;
        }

        public void Unmap()
        {
            _gl.UnmapBuffer(_target);

            EndUpdate();
        }


        public unsafe void Update(T value)
        {
            if (value is IDynamicBuffer dynamic)
            {
                using var dynBuffer = dynamic.GetBuffer();
                Update((void*)dynBuffer.Data, dynBuffer.Size, false);
            }
            else
            {
                Update(&value, (uint)sizeof(T), true);
            }
        }

        public unsafe void UpdateRange(ReadOnlySpan<T> value, int dstIndex = 0)
        {
            if (value.Length == 0)
                return;

            var sizeBytes = (uint)(value.Length * sizeof(T));

            fixed (T* pData = &value[0])
                UpdateRange(pData, sizeBytes, dstIndex * sizeof(T), true);
        }

        unsafe void IBuffer.Update(object value)
        {
            if (value is T tValue)
                Update(tValue);
            else
                throw new NotSupportedException();
        }

        unsafe void IBuffer.UpdateRange(ReadOnlySpan<byte> value, int dstIndex = 0)
        {
            fixed (byte* pData = &value[0])
                UpdateRange(pData, (uint)value.Length, dstIndex * sizeof(T), true);
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
                Unbind();
                _gl.DeleteBuffer(_handle);
            }

            base.Dispose();
        }


        public string Hash { get; set; }

        public long Version { get; set; }

        public int Slot { get; set; }

        public BufferTargetARB Target => _target;

        public unsafe uint ArrayLength => (uint)(_sizeBytes / sizeof(T));

        public uint SizeBytes => _sizeBytes;
    }
}
