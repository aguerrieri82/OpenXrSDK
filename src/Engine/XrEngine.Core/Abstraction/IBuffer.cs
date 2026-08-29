namespace XrEngine
{

    public unsafe interface IBufferLock : IDisposable
    {
        void* Data { get; }
    }

    [Flags]
    public enum BufferAccessMode
    {
        Read = 0x1,
        Write = 0x2,
        Replace = 0x4 | Write,
        ReadWrite = Read | Write
    }

    [Flags]
    public enum BufferAllocateFlags
    {
        None = 0,
        Mutable = 0x1,
        Persistent = 0x2,
        PersistentRead = 0x4 | Persistent,
        PersistentWrite = 0x8 | Persistent,
    }

    public interface ISimpleBuffer
    {
        uint SizeBytes { get; }

        void Update(object value);

        long Version { get; set; }

        uint Handle { get; }
    }

    public interface ISimpleBuffer<T> : ISimpleBuffer
    {
        void Update(in T value);
    }

    public interface IBuffer : ISimpleBuffer
    {
        void BeginUpdate();

        void EndUpdate();

        void UpdateRange(ReadOnlySpan<byte> value, int dstIndex = 0, bool preserve = true);

        void Allocate(uint sizeInByte, BufferAllocateFlags flags = BufferAllocateFlags.Mutable);

        IBufferLock Lock(BufferAccessMode mode);
    }

    public interface IBuffer<T> : IBuffer, ISimpleBuffer<T>
    {
        void UpdateRange(ReadOnlySpan<T> value, int dstIndex = 0, bool preserve = true);

        void ReadArray(ref T[] result);
    }

}
