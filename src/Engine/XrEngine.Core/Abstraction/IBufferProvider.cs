namespace XrEngine
{
    public interface IBufferProvider
    {
        IBuffer GetBuffer<T>(string name, bool isGlobal);
    }
}
