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

    public unsafe interface IBuffer
    {
        void BeginUpdate();

        void EndUpdate();

        void Update(object value);

        void UpdateRange(ReadOnlySpan<byte> value, int dstIndex = 0);

        void Allocate(uint sizeInByte);

        byte* Lock(BufferAccessMode mode);

        void Unlock();

        string Hash { get; set; }

        long Version { get; set; }

        uint SizeBytes { get; }
    }

    public interface IBuffer<T> : IBuffer
    {
        void Update(T value);

        void UpdateRange(ReadOnlySpan<T> value, int dstIndex);
    }

}
