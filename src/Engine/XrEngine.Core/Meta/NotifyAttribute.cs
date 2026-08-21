namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NotifyAttribute : Attribute
    {
        public NotifyAttribute(ChangeType type)
        {
            Type = type;
        }

        public ChangeType Type { get; }
    }
}
