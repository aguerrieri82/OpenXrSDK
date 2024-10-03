namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SaveStateAttribute(bool isSave) : Attribute
    {
        public bool IsSave { get; } = isSave;
    }
}
