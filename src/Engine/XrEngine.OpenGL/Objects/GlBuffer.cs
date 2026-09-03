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
        public static IGlBuffer Create(GL gl, BufferTargetARB target, Type contentType)
        {
            var type = typeof(GlBuffer<>).MakeGenericType(contentType);
            return (IGlBuffer)Activator.CreateInstance(type, gl, target)!;
        }

        public static GlBufferUpdateTracker? Tracker;
    }

    public unsafe class GlBufferMap<T> : IBufferLock 
        where T : unmanaged
    {
        readonly GlBuffer<T> _buffer;
        readonly T* _data;

        public GlBufferMap(GlBuffer<T> buffer, MapBufferAccessMask accessMask)
        {
            _buffer = buffer;
            _data = _buffer.MapRange(0, _buffer.SizeBytes, accessMask);
            if (_data == null)
                throw new InvalidOperationException("MapRange failed");
        }

        public void Dispose()
        {
            _buffer.Unmap();
        }

        public T* Data => _data;

        public Span<T> Span => new(_data, (int)_buffer.SizeBytes / sizeof(T));

        void* IBufferLock.Data => _data;
    }

    public class GlBuffer<T> : GlObject, IGlBuffer, IBuffer<T> 
        where T : unmanaged
    {
        protected readonly BufferTargetARB _target;
        protected BufferUsageARB _usage;
        protected uint _capacityBytes;
        protected uint _sizeBytes;
        protected int _beginUpdateCount;
        protected long _updateCount;
        private BufferStorageMask _storageMask;
        protected readonly uint _elementSize;
        protected BufferAllocateFlags _allocateFlags = BufferAllocateFlags.Mutable;
        protected bool _isMapped;

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
            _allocateFlags = flags;

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
                    {
                        ResizeStorage(sizeInByte, preserve: false);
                        return;
                    }

                    AllocateImmutableStorage(sizeInByte, flags);
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

            ResizeStorage(requiredBytes, preserve);
        }

        private unsafe void ReallocateStorage(uint newCapacityBytes)
        {
            if (IsMutable)
            {
                _gl.BufferData(_target, newCapacityBytes, null, _usage);
            }
            else
            {
                if (_capacityBytes > 0)
                    Recreate();

                Bind();

                AllocateImmutableStorage(newCapacityBytes, _allocateFlags);
            }
        }

        private unsafe void ResizeStorage(uint newCapacityBytes, bool preserve)
        {
            var copySizeBytes = Math.Min(_sizeBytes, newCapacityBytes);

            if (!preserve || copySizeBytes == 0)
            {
                ReallocateStorage(newCapacityBytes);

                _capacityBytes = newCapacityBytes;
                _sizeBytes = newCapacityBytes;

                return;
            }

            var useCpuCopy = !IsMutable;

#if GLES
            useCpuCopy = true;
#endif

            //useCpuCopy = true;

            if (useCpuCopy)
            {
                if (_isMapped)
                    Unmap();

                var data = new T[ArrayLength];

                ReadArray(ref data);

                ReallocateStorage(newCapacityBytes);

                fixed (T* pData = data)
                    _gl.BufferSubData(_target, 0, copySizeBytes, pData);
            }
            else
            {
                var oldHandle = _handle;

                Create();

                var glState = GlState.Current;

                glState.RemoveBufferRef(oldHandle);

                glState.BindBuffer(BufferTargetARB.CopyWriteBuffer, _handle);

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

                _gl.DeleteBuffer(oldHandle);
            }

            _capacityBytes = newCapacityBytes;
            _sizeBytes = copySizeBytes;
        }

        private unsafe void AllocateImmutableStorage(uint sizeBytes, BufferAllocateFlags flags)
        {
            _storageMask =
                BufferStorageMask.DynamicStorageBit |
                BufferStorageMask.MapReadBitExt;

            if ((flags & BufferAllocateFlags.Persistent) != 0)
            {
                _storageMask |=
                    BufferStorageMask.MapPersistentBit |
                    BufferStorageMask.MapCoherentBit;

                if ((flags & BufferAllocateFlags.PersistentWrite) != 0)
                    _storageMask |= BufferStorageMask.MapWriteBit;
            }

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

        public unsafe T* MapRange(uint offsetInBytes, uint sizeBytes, MapBufferAccessMask access)
        {
            if (_isMapped)
                throw new InvalidOperationException("Buffer is already mapped.");

            if (sizeBytes == 0)
                throw new InvalidOperationException("Cannot map an empty buffer.");

            BeginUpdate();

            var ptr = _gl.MapBufferRange(
                _target,
                (nint)offsetInBytes,
                sizeBytes,
                access);

            EndUpdate();

            _isMapped = true;

            return (T*)ptr;
        }

        public void Unmap()
        {
            if (!_isMapped)
                throw new InvalidOperationException("Buffer is not mapped.");

            BeginUpdate();

            _gl.UnmapBuffer(_target);

            _isMapped = false;

            EndUpdate();
        }

        public GlBufferMap<T> Map(MapBufferAccessMask access)
        {
            return new GlBufferMap<T>(this, access);
        }

        IBufferLock IBuffer.Lock(BufferAccessMode mode)
        {
            var access = mode switch
            {
                BufferAccessMode.Read => MapBufferAccessMask.ReadBit,

                BufferAccessMode.Write => MapBufferAccessMask.WriteBit,

                BufferAccessMode.Replace => MapBufferAccessMask.WriteBit |
                                            MapBufferAccessMask.InvalidateBufferBit,

                BufferAccessMode.ReadWrite => MapBufferAccessMask.ReadBit |
                                              MapBufferAccessMask.WriteBit,

                _ => throw new NotSupportedException()
            };

            return Map(access);
        }

        public void Read(ref T result)
        {
            using var map = Map(MapBufferAccessMask.ReadBit);
            result = map.Span[0];
        }

        public void ReadArray(ref T[] result)
        {
            var arrayLength = ArrayLength;

            if (result == null || result.Length != arrayLength)
                result = new T[arrayLength];

            if (arrayLength == 0)
                return;

            using var map = Map(MapBufferAccessMask.ReadBit);

            map.Span.CopyTo(result);
        }

        public unsafe void Update(in T value)
        {
            IsMutable = false;

            fixed (T* pValue = &value)
                Update(pValue, _elementSize);
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

        protected void Recreate()
        {
            Destroy();
            Create();
        }

        protected void Destroy()
        {
            if (_isMapped)
                Unmap();

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