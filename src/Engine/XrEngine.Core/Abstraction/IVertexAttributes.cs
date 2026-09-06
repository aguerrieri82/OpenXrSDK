namespace XrEngine
{
    public struct VertexAttributesBuffer
    {
        public uint BaseLocation;

        public VertexComponent Component;

        public Array Data;

        public Type? ElementType;
    }

    public interface IVertexAttributes
    {
        int BufferCount { get; }

        VertexAttributesBuffer GetBuffer(int index);
    }
}
