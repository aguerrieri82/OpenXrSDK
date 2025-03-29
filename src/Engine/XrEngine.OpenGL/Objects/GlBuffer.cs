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
        protected uint _arrayLength;
        protected BufferUsageARB _usage;

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

            _usage = _target switch
            {
                BufferTargetARB.UniformBuffer => BufferUsageARB.StreamDraw,
                BufferTargetARB.ShaderStorageBuffer => BufferUsageARB.DynamicDraw,
                _ => BufferUsageARB.StaticDraw
            };
        }

        public unsafe void Update(nint data, uint sizeBytes, bool wait)
        {
            Bind();

            var newArrayLen = sizeBytes / (uint)sizeof(T);

            if (_arrayLength != newArrayLen || _target == BufferTargetARB.UniformBuffer)
            {
                _gl.BufferData(_target, sizeBytes, (void*)data, _usage);
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

        unsafe byte* IBuffer.Lock(BufferAccessMode mode)
        {
            var mask = mode switch
            {
                BufferAccessMode.Read => MapBufferAccessMask.ReadBit,
                BufferAccessMode.Write => MapBufferAccessMask.WriteBit,
                BufferAccessMode.Replace => MapBufferAccessMask.WriteBit| MapBufferAccessMask.InvalidateBufferBit,
                BufferAccessMode.ReadWrite => MapBufferAccessMask.ReadBit | MapBufferAccessMask.WriteBit,
                _ => throw new NotSupportedException()
            };

            Bind();
            return (byte*)Map(mask);
        }

        void IBuffer.Unlock()
        {
            Unmap();
            Unbind();
        }

        public unsafe void Resize(uint sizeInByte)
        {
            var newArrayLen = sizeInByte / (uint)sizeof(T);

            if (_arrayLength == newArrayLen)
                return;

            Bind();
            _gl.BufferData(_target, sizeInByte, null, _usage);
            _arrayLength = newArrayLen;
            Unbind();
        }

        //TODO: duplicated

        public unsafe void Allocate(uint length)
        {
            if (_arrayLength == length)
                return;

            _gl.BufferData(_target, (nuint)(length * sizeof(T)), null, _usage);
            _arrayLength = length;
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
            if (value is T tValue)
                Update(tValue);
            else
                throw new NotSupportedException();
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
            }

            base.Dispose();
        }


  
        public string Hash { get; set; }

        public long Version { get; set; }

        public int Slot { get; set; }

        public BufferTargetARB Target => _target;

        public uint ArrayLength => _arrayLength;

    }
}
