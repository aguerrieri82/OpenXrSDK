namespace OpenXr.Engine
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShaderRefAttribute : Attribute
    {
        public ShaderRefAttribute(uint loc, string name)
        {
            Loc = loc;
            Name = name;
        }

        public uint Loc { get; }

        public string Name { get; }
    }
}
