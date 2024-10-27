namespace XrEngine
{
    public enum ValueType
    {
        None,
        Radiant,
        FileName
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
