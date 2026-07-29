#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using Common.Interop;


namespace XrEngine.OpenGL
{
    public static class GlBuffer
    {
        public static IBuffer Create(GL gl, BufferTargetARB target, Type contentType)
        {
            var type = typeof(GlBuffer<>).MakeGenericType(contentType);
            return (IBuffer)Activator.CreateInstance(type, gl, target)!;
        }

        public static GlBufferUpdateTracker? Tracker;
    }

    public class GlBuffer<T> : GlObject, IGlBuffer, IBuffer<T>
    {
        protected readonly BufferTargetARB _target;
        protected BufferUsageARB _usage;
        protected uint _capacityBytes;
        protected uint _sizeBytes;
        protected int _beginUpdateCount;
        protected long _updateCount;
        private BufferStorageMask _storageMask;
        protected readonly uint _elementSize;



#if GLES
        private Silk.NET.OpenGLES.Extensions.EXT.ExtBufferStorage? _storageExt;
#endif

        public GlBuffer(GL gl, BufferTargetARB target)
            : base(gl)
        {
            _target = target;
            _usage = BufferUsageARB.StaticDraw;

            GlBuffer.Tracker ??= new GlBufferUpdateTracker(gl);

            Version = -1;
            ActiveSlot = 0;
            IsMutable = true;
           
            _elementSize = (uint)MarshalCache.SizeOf(typeof(T));

            Create();
        }

        public GlBuffer(GL gl, ReadOnlySpan<T> data, BufferTargetARB target)
            : this(gl, target)
        {
            UpdateRange(data);
        }

        protected void Create()
        {
            _handle = _gl.GenBuffer();
             SetLabel(typeof(T).Name);
            CreateVersion++;
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
                var promote =
                    _updateCount > 50 &&
                    _usage == BufferUsageARB.StaticDraw &&
                    IsMutable;

                if (promote)
                {
                    _usage = BufferUsageARB.DynamicDraw;

                    _capacityBytes = Math.Max(_capacityBytes, sizeBytes);

                    _gl.BufferData(_target, _capacityBytes, null, _usage);
                }
                else
                    EnsureCapacity(sizeBytes, preserve: false);

                if (sizeBytes > 0)
                    _gl.BufferSubData(_target, 0, sizeBytes, data);

                _sizeBytes = sizeBytes;

                _updateCount++;
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

            if (data == null)
                return;

            if (GlDebug.TrackBuffers)
                _gl.ClearError();

            var writeEndBytes = (uint)offsetBytes + sizeBytes;

            BeginUpdate();

            try
            {
                var destructiveResize = !preserve && writeEndBytes > _capacityBytes;

                var promote =
                    _updateCount > 50 &&
                    _usage == BufferUsageARB.StaticDraw &&
                    !preserve &&
                    IsMutable;

                if (promote)
                {
                    _usage = BufferUsageARB.DynamicDraw;
                    _capacityBytes = Math.Max(_capacityBytes, writeEndBytes);
                    _gl.BufferData(_target, _capacityBytes, null, _usage);
                }
                else
                    EnsureCapacity(writeEndBytes, preserve);

                _gl.BufferSubData(_target, offsetBytes, sizeBytes, data);

                if (promote || destructiveResize)
                    _sizeBytes = writeEndBytes;
                else
                    _sizeBytes = Math.Max(_sizeBytes, writeEndBytes);

                if (GlDebug.TrackBuffers)
                {
                    GlBuffer.Tracker!.Update(this, _target, data, sizeBytes, offsetBytes);

                    _gl.CheckError();
          
                    var active = _gl.GetActiveBufferBinding(_target);
                    if (active != _handle)
                        Log.Error(this, "Inconsistent BUF cache for {0}: Real Active {1} - Expected {2}", _target, active, _handle);
                }

                _updateCount++;
            }
            finally
            {
                EndUpdate();
            }
        }

        public void BeginUpdate()
        {
            if (_beginUpdateCount == 0)
                Bind();

            _beginUpdateCount++;
        }

        public void EndUpdate()
        {
            Debug.Assert(_beginUpdateCount > 0);

            _beginUpdateCount--;

            if (_beginUpdateCount == 0)
                Unbind();
        }

        public unsafe void Allocate(uint sizeInByte, BufferAllocateFlags flags = BufferAllocateFlags.Mutable)
        {
            IsMutable = (flags & BufferAllocateFlags.Mutable) != 0;

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

                    AllocateImmutableStorage(sizeInByte, flags);
                }
                else
                    _gl.BufferData(_target, sizeInByte, null, _usage);

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
            var copySizeBytes = Math.Min(_sizeBytes, newCapacityBytes);

            if (!preserve || copySizeBytes == 0)
            {
                Allocate(newCapacityBytes, IsMutable ? BufferAllocateFlags.Mutable : BufferAllocateFlags.None);
                return;
            }

#if GLES

#warning CopyBufferSubData DOES NOT work on Quest and does not report any error; fallback to CPU copy.

            var data = new T[ArrayLength];

            BeginUpdate();

            ReadArray(ref data);

            _gl.BufferData(_target, newCapacityBytes, null, _usage);

            fixed (T* pData = data)
                _gl.BufferSubData(_target, 0, copySizeBytes, pData);

            EndUpdate();
#else
            var oldHandle = _handle;
            var newHandle = _gl.GenBuffer();

            var glState = GlState.Current;

            glState.RemoveBufferRef(newHandle);

            glState.BindBuffer(BufferTargetARB.CopyWriteBuffer, newHandle);

            _gl.BufferData(
                BufferTargetARB.CopyWriteBuffer,
                newCapacityBytes,
                null,
                _usage);

            glState.BindBuffer(BufferTargetARB.CopyReadBuffer, oldHandle);

            _gl.CopyBufferSubData(
                CopyBufferSubDataTarget.CopyReadBuffer,
                CopyBufferSubDataTarget.CopyWriteBuffer,
                0,
                0,
                copySizeBytes);

            glState.BindBuffer(BufferTargetARB.CopyReadBuffer, 0);
            glState.BindBuffer(BufferTargetARB.CopyWriteBuffer, 0);

            _handle = newHandle;
            _gl.DeleteBuffer(oldHandle);

            CreateVersion++;
            
#endif
            _capacityBytes = newCapacityBytes;
            _sizeBytes = copySizeBytes;
        }

        private unsafe void AllocateImmutableStorage(uint sizeBytes, BufferAllocateFlags flags)
        {
            _storageMask = 0;
            
            if ((flags & BufferAllocateFlags.Persistent) != 0)
            {
                _storageMask |= BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit;
                
                if ((flags & BufferAllocateFlags.PersistentRead) != 0)
                    _storageMask |= BufferStorageMask.MapReadBitExt;

                if ((flags & BufferAllocateFlags.PersistentWrite) != 0)
                    _storageMask |= BufferStorageMask.MapWriteBit;
            }
            else
                _storageMask = BufferStorageMask.DynamicStorageBit;

#if GLES
            if (_storageExt == null && !_gl.TryGetExtension(out _storageExt))
                throw new NotSupportedException("GL_EXT_buffer_storage not supported");


            _storageExt!.BufferStorage(
                (BufferStorageTarget)_target,
                sizeBytes,
                null,
                _storageMask);
#else
            _gl.BufferStorage(
                (GLEnum)_target,
                sizeBytes,
                null,
                _storageMask);
#endif
        }

        public unsafe T* MapPermanentRead()
        {
            return MapPermanent(MapBufferAccessMask.ReadBit);
        }

        public unsafe T* MapPermanent(MapBufferAccessMask access)
        {
            return MapPermanent(0, _sizeBytes, access);
        }

        public unsafe T* MapPermanent(uint offsetInBytes, uint sizeBytes, MapBufferAccessMask access)
        {
            if (IsMutable || _sizeBytes == 0 || (_storageMask & BufferStorageMask.MapPersistentBit) == 0)
                throw new InvalidOperationException();

            Bind();

            access |= MapBufferAccessMask.PersistentBit | MapBufferAccessMask.CoherentBit;

            var ptr = _gl.MapBufferRange(
                _target,
                0,
                _sizeBytes,
                access);

            Unbind();

            return (T*)ptr;

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
            var arrayLength = ArrayLength;

            if (result == null || result.Length != arrayLength)
                result = new T[arrayLength];

            if (arrayLength == 0)
                return;

            var ptr = Map(MapBufferAccessMask.ReadBit);

            try
            {
                fixed (T* pResult = result)
                {
                    var sizeBytes = (uint)(_elementSize * result.Length);

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

                Update((void*)dynamicBuffer.Data, dynamicBuffer.Size);

                return;
            }

            IsMutable = false;

            Update(&value, _elementSize);
        }

        public unsafe void UpdateRange(
            ReadOnlySpan<T> value,
            int dstIndex = 0,
            bool preserve = true)
        {
            Debug.Assert(dstIndex >= 0);

            if (value.Length == 0)
                return;

            var sizeBytes = (uint)(value.Length * _elementSize);

            var offsetBytes = (int)(dstIndex * _elementSize);

            fixed (T* pData = value)
                UpdateRange(pData, sizeBytes, offsetBytes, preserve);
        }

        void ISimpleBuffer.Update(object value)
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

            var offsetBytes = (int)(dstIndex * _elementSize);

            fixed (byte* pData = value)
                UpdateRange(pData, (uint)value.Length, offsetBytes, preserve);
        }

        public void Bind()
        {
            GlState.Current.BindBuffer(_target, _handle);
        }

        public void Unbind()
        {
            GlState.Current.BindBuffer(_target, 0);
        }

        protected void Destroy()
        {
            Unbind();

            _gl.DeleteBuffer(_handle);

            GlState.Current.RemoveBufferRef(_handle);

            _handle = 0;
        }

        public override void Dispose()
        {
            if (_handle != 0)
            {
                Destroy();

                GlDebug.Log(
                    this,
                    "Buffer {0} ({1}) deleted",
                    _handle,
                    Target);
            }

            base.Dispose();
        }

        public void Load(int slot)
        {
            GlState.Current?.LoadBuffer(this, slot);
        }

        public long CreateVersion { get; set; }

        public bool IsMutable { get; set; }

        public long Version { get; set; }

        public int ActiveSlot { get; set; }

        public BufferTargetARB Target => _target;

        public uint ArrayLength
        {
            get => (_sizeBytes / _elementSize);
            set => SizeBytes = value * _elementSize;
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