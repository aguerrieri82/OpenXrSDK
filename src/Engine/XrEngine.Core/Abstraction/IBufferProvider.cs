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
        ISimpleBuffer<T> GetBuffer<T>(int bufferId, BufferStore store, BufferUsage usage = BufferUsage.Uniforms, string? uniformName = "")
            where T : unmanaged;
    }
}
