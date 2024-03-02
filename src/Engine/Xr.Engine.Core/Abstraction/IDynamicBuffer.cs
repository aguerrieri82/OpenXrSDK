namespace Xr.Engine
{
    public unsafe struct DynamicBuffer : IDisposable
    {
        public nint Data;

        public int Size;

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
