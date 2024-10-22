namespace XrEngine
{
    public enum ValueType
    {
        None,
        Radiant
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ValueTypeAttribute : Attribute
    {
        public ValueTypeAttribute(ValueType type)
        {
            Type = type;
        }

        public ValueType Type { get; }
    }
}
