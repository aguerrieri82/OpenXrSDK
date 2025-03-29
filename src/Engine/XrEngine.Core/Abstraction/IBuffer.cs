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
        void Update(object value);

        void Resize(uint sizeInByte);

        byte* Lock(BufferAccessMode mode);

        void Unlock();  

        string Hash { get; set; }

        long Version { get; set; }
    }

    public interface IBuffer<T> : IBuffer
    {
        void Update(T value);


    }

}
