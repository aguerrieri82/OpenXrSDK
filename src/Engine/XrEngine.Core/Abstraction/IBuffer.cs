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

    public interface ISimpleBuffer
    {
        void Update(object value);

        long Version { get; set; }
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

        void Allocate(uint sizeInByte);

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
