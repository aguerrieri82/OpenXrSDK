#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;

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
        protected readonly BufferUsageARB _usage;

        protected uint _capacityBytes;
        protected uint _sizeBytes;
        protected int _updateCount;

#if GLES
        private Silk.NET.OpenGLES.Extensions.EXT.ExtBufferStorage? _storageExt;
#endif

        public GlBuffer(GL gl, BufferTargetARB target)
            : base(gl)
        {
            _target = target;

            _usage = target switch
            {
                BufferTargetARB.UniformBuffer => BufferUsageARB.DynamicDraw,
                BufferTargetARB.ShaderStorageBuffer => BufferUsageARB.DynamicDraw,
                _ => BufferUsageARB.StaticDraw
            };

            Version = -1;
            ActiveSlot = 0;
            IsMutable = true;

            _handle = _gl.GenBuffer();
        }

        public GlBuffer(GL gl, ReadOnlySpan<T> data, BufferTargetARB target)
            : this(gl, target)
        {
            UpdateRange(data);
        }

        public void Update(Func<(T, bool)> getValue)
        {
#if GL_WRAPPER
            _gl.BufferData(value =>
            {
                var (actualValue, hasValue) = value;

                if (hasValue)
                    Update(actualValue);
            }, getValue);
#else
            var (actualValue, hasValue) = getValue();

            if (hasValue)
                Update(actualValue);
#endif
        }

        public unsafe void Update(void* data, uint sizeBytes)
        {
            BeginUpdate();

            try
            {
                EnsureCapacity(sizeBytes, preserve: false);

                if (sizeBytes > 0)
                    _gl.BufferSubData(_target, 0, sizeBytes, data);

                _sizeBytes = sizeBytes;
            }
            finally
            {
                EndUpdate();
            }
        }

        public unsafe void UpdateRange(void* data, uint sizeBytes, int offsetBytes, bool preserve)
        {
            Debug.Assert(offsetBytes >= 0);

            if (sizeBytes == 0)
                return; 

            uint writeEndBytes = checked((uint)offsetBytes + sizeBytes);

            BeginUpdate();

            try
            {
                EnsureCapacity(writeEndBytes, preserve);

                _gl.BufferSubData(_target, offsetBytes, sizeBytes, data);

                if (writeEndBytes > _sizeBytes)
                    _sizeBytes = writeEndBytes;
            }
            finally
            {
                EndUpdate();
            }
        }

        public void BeginUpdate()
        {
            if (_updateCount == 0)
                Bind();

            _updateCount++;
        }

        public void EndUpdate()
        {
            Debug.Assert(_updateCount > 0);

            _updateCount--;

            if (_updateCount == 0)
                Unbind();
        }

        public unsafe void Allocate(uint sizeInByte)
        {
            if (_capacityBytes == sizeInByte)
            {
                _sizeBytes = sizeInByte;
                return;
            }

            BeginUpdate();

            try
            {
                if (!IsMutable)
                {
                    if (_capacityBytes != 0)
                        throw new InvalidOperationException(
                            $"Immutable buffer size changed: OLD: {_capacityBytes} NEW: {sizeInByte}");

                    AllocateImmutableStorage(sizeInByte);
                }
                else
                {
                    _gl.BufferData(_target, sizeInByte, null, _usage);
                }

                _capacityBytes = sizeInByte;
                _sizeBytes = sizeInByte;
            }
            finally
            {
                EndUpdate();
            }
        }

        public void Resize(uint newSizeBytes, bool preserve)
        {
            if (_capacityBytes == newSizeBytes)
            {
                if (_sizeBytes > newSizeBytes)
                    _sizeBytes = newSizeBytes;

                return;
            }

            if (!IsMutable && _capacityBytes != 0)
            {
                throw new InvalidOperationException(
                    $"Immutable buffer size changed: OLD: {_capacityBytes} NEW: {newSizeBytes}");
            }

            BeginUpdate();

            try
            {
                ResizeStorage(newSizeBytes, preserve);
            }
            finally
            {
                EndUpdate();
            }
        }

        private void EnsureCapacity(uint requiredBytes, bool preserve)
        {
            if (requiredBytes <= _capacityBytes)
                return;

            if (!IsMutable && _capacityBytes != 0)
            {
                throw new InvalidOperationException(
                    $"Immutable buffer size changed: OLD: {_capacityBytes} NEW: {requiredBytes}");
            }

            ResizeStorage(requiredBytes, preserve);
        }

        private unsafe void ResizeStorage(uint newCapacityBytes, bool preserve)
        {
            uint copySizeBytes = Math.Min(_sizeBytes, newCapacityBytes);

            if (!preserve || copySizeBytes == 0)
            {
                _gl.BufferData(_target, newCapacityBytes, null, _usage);

                _capacityBytes = newCapacityBytes;
                _sizeBytes = copySizeBytes;
                return;
            }

            uint oldHandle = _handle;
            uint newHandle = _gl.GenBuffer();

            GlState.Current!.BindBuffer(BufferTargetARB.CopyWriteBuffer, newHandle);
            _gl.BufferData(
                BufferTargetARB.CopyWriteBuffer,
                newCapacityBytes,
                null,
                _usage);

            GlState.Current.BindBuffer(BufferTargetARB.CopyReadBuffer, oldHandle);

            _gl.CopyBufferSubData(
                CopyBufferSubDataTarget.CopyReadBuffer,
                CopyBufferSubDataTarget.CopyWriteBuffer,
                0,
                0,
                copySizeBytes);

            GlState.Current.BindBuffer(BufferTargetARB.CopyReadBuffer, 0);
            GlState.Current.BindBuffer(BufferTargetARB.CopyWriteBuffer, 0);

            _handle = newHandle;
            _gl.DeleteBuffer(oldHandle);

            _capacityBytes = newCapacityBytes;
            _sizeBytes = copySizeBytes;

            GlState.Current.BindBuffer(_target, _handle);
        }

        private unsafe void AllocateImmutableStorage(uint sizeBytes)
        {
#if GLES
            if (_storageExt == null &&
                !_gl.TryGetExtension(out _storageExt))
            {
                throw new NotSupportedException(
                    "GL_EXT_buffer_storage not supported");
            }

            _storageExt!.BufferStorage(
                (BufferStorageTarget)_target,
                sizeBytes,
                null,
                BufferStorageMask.DynamicStorageBit);
#else
            _gl.BufferStorage(
                (GLEnum)_target,
                sizeBytes,
                null,
                BufferStorageMask.DynamicStorageBit);
#endif
        }

        public unsafe T* Map(MapBufferAccessMask access)
        {
            if (_sizeBytes == 0)
                throw new InvalidOperationException("Cannot map an empty buffer.");

            BeginUpdate();

            try
            {
                var ptr = _gl.MapBufferRange(
                    _target,
                    0,
                    _sizeBytes,
                    access);

                if (ptr == null)
                    throw new InvalidOperationException(
                        "MapBufferRange returned NULL.");

                return (T*)ptr;
            }
            catch
            {
                EndUpdate();
                throw;
            }
        }

        public void Unmap()
        {
            try
            {
                _gl.UnmapBuffer(_target);
            }
            finally
            {
                EndUpdate();
            }
        }

        unsafe byte* IBuffer.Lock(BufferAccessMode mode)
        {
            var access = mode switch
            {
                BufferAccessMode.Read =>
                    MapBufferAccessMask.ReadBit,

                BufferAccessMode.Write =>
                    MapBufferAccessMask.WriteBit,

                BufferAccessMode.Replace =>
                    MapBufferAccessMask.WriteBit |
                    MapBufferAccessMask.InvalidateBufferBit,

                BufferAccessMode.ReadWrite =>
                    MapBufferAccessMask.ReadBit |
                    MapBufferAccessMask.WriteBit,

                _ => throw new NotSupportedException()
            };

            return (byte*)Map(access);
        }

        void IBuffer.Unlock()
        {
            Unmap();
        }

        public unsafe void Read(ref T result)
        {
            var ptr = Map(MapBufferAccessMask.ReadBit);

            try
            {
                result = *ptr;
            }
            finally
            {
                Unmap();
            }
        }

        public unsafe void ReadArray(ref T[] result)
        {
            uint arrayLength = ArrayLength;

            if (result == null || result.Length != arrayLength)
                result = new T[arrayLength];

            if (arrayLength == 0)
                return;

            var ptr = Map(MapBufferAccessMask.ReadBit);

            try
            {
                fixed (T* pResult = result)
                {
                    uint sizeBytes = checked((uint)(sizeof(T) * result.Length));

                    System.Buffer.MemoryCopy(
                        ptr,
                        pResult,
                        sizeBytes,
                        sizeBytes);
                }
            }
            finally
            {
                Unmap();
            }
        }

        public unsafe void Update(T value)
        {
            if (value is IDynamicBuffer dynamicBufferSource)
            {
                using var dynamicBuffer = dynamicBufferSource.GetBuffer();

                Update(
                    (void*)dynamicBuffer.Data,
                    dynamicBuffer.Size);

                return;
            }

            if (_capacityBytes == 0)
            {
                // Preserve the existing behavior: a normal typed first update
                // produces fixed-capacity immutable storage.
                IsMutable = false;
                Allocate((uint)sizeof(T));
            }

            Update(&value, (uint)sizeof(T));
        }

        public unsafe void UpdateRange(
            ReadOnlySpan<T> value,
            int dstIndex = 0,
            bool preserve = true)
        {
            Debug.Assert(dstIndex >= 0);

            if (value.Length == 0)
                return;

            uint sizeBytes = checked((uint)(value.Length * sizeof(T)));
            int offsetBytes = checked(dstIndex * sizeof(T));

            fixed (T* pData = value)
                UpdateRange(pData, sizeBytes, offsetBytes, preserve);
        }

        void IBuffer.Update(object value)
        {
            if (value is not T typedValue)
                throw new NotSupportedException();

            Update(typedValue);
        }

        void IBuffer.Update(Func<object?> getValue)
        {
            Update(() =>
            {
                var value = getValue();

                if (value == null)
                    return (default!, false);

                if (value is T typedValue)
                    return (typedValue, true);

                throw new NotSupportedException();
            });
        }

        unsafe void IBuffer.UpdateRange(
            ReadOnlySpan<byte> value,
            int dstIndex,
            bool preserve)
        {
            Debug.Assert(dstIndex >= 0);

            if (value.Length == 0)
                return;

            int offsetBytes = checked(dstIndex * sizeof(T));

            fixed (byte* pData = value)
                UpdateRange(pData, (uint)value.Length, offsetBytes, preserve);
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

                GlDebug.Log(
                    this,
                    "Buffer {0} ({1}) deleted",
                    _handle,
                    Target);
            }

            base.Dispose();
        }

        public bool IsMutable { get; set; }

        public long Version { get; set; }

        public int ActiveSlot { get; set; }

        public BufferTargetARB Target => _target;

        public unsafe uint ArrayLength
        {
            get => (uint)(_sizeBytes / sizeof(T));
            set => SizeBytes = checked(value * (uint)sizeof(T));
        }

        public uint SizeBytes
        {
            get => _sizeBytes;
            set
            {
                if (value > _capacityBytes)
                {
                    throw new InvalidOperationException(
                        $"Logical buffer size {value} exceeds capacity {_capacityBytes}.");
                }

                _sizeBytes = value;
            }
        }
    }
}