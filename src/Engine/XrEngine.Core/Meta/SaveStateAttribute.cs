namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SaveStateAttribute(bool isSave = true) : Attribute
    {
        public bool IsSave { get; } = isSave;
    }
}
