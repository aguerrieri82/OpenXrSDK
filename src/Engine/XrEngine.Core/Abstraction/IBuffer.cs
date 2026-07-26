namespace XrEngine
{
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
        Persistent =0x2,
        PersistentRead = 0x4 | Persistent,
        PersistentWrite = 0x8 | Persistent,
    }

    public interface ISimpleBuffer
    {
        void Update(object value);

        long Version { get; set; }

        uint Handle { get; }
    }

    public interface ISimpleBuffer<T> : ISimpleBuffer
    {
        void Update(T value);
    }

    public unsafe interface IBuffer : ISimpleBuffer
    {
        void BeginUpdate();

        void EndUpdate();

        void Update(Func<object?> value);

        void UpdateRange(ReadOnlySpan<byte> value, int dstIndex = 0, bool preserve = true);

        void Allocate(uint sizeInByte, BufferAllocateFlags flags = BufferAllocateFlags.Mutable);

        byte* Lock(BufferAccessMode mode);

        void Unlock();

        uint SizeBytes { get; }
    }

    public interface IBuffer<T> : IBuffer, ISimpleBuffer<T>
    {
        void Update(Func<(T, bool)> getValue);

        void UpdateRange(ReadOnlySpan<T> value, int dstIndex = 0, bool preserve = true);

        void ReadArray(ref T[] result);
    }

}
