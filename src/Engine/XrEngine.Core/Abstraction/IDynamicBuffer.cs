namespace XrEngine
{
    public unsafe struct DynamicBuffer : IDisposable
    {
        public nint Data;

        public uint Size;

        public Action? Free;

        public void Dispose()
        {
            Free?.Invoke();
        }
    }

    public interface IDynamicBuffer
    {
        DynamicBuffer GetBuffer();
    }
}
