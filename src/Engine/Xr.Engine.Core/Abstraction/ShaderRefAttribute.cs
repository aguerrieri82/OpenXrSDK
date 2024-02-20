namespace OpenXr.Engine
{
    public enum VertexComponent
    {
        Generic,
        Position,
        Normal,
        Tangent,
        Color3,
        Color4,
        UV0,
        UV1,
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ShaderRefAttribute : Attribute
    {
        public ShaderRefAttribute(uint loc, string name, VertexComponent component)
        {
            Location = loc;
            Name = name;
            Component= component;   
        }

        public uint Location { get; }

        public string Name { get; }

        public VertexComponent Component { get; }
    }
}
