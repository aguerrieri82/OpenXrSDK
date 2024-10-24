namespace XrEngine
{
    public enum BufferStore
    {
        Shader,
        Material,
        Model
    }

    public interface IBufferProvider
    {
        IBuffer GetBuffer<T>(int bufferId, BufferStore store);
    }
}
