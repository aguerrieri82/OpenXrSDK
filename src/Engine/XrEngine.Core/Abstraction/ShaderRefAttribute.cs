namespace XrEngine
{
    [Flags]
    public enum VertexComponent
    {
        None = 0,
        Generic = 0x1,
        Position = 0x2,
        Normal = 0x4,
        Tangent = 0x8,
        Color3 = 0x10,
        Color4 = 0x20,
        UV0 = 0x40,
        UV1 = 0x80,
        Size = 0x100,
        JointIndex = 0x200,
        JointWeight = 0x400,
        Skin = JointIndex | JointWeight
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ShaderRefAttribute : Attribute
    {
        public ShaderRefAttribute(uint loc, string name, VertexComponent component)
        {
            Location = loc;
            Name = name;
            Component = component;
        }

        public uint Location { get; }

        public string Name { get; }

        public bool IsNormalized { get; set; }

        public bool IsIntegerStore { get; set; }

        public VertexComponent Component { get; }
    }
}
