namespace XrEngine
{

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class EditableAttribute : Attribute
    {
        public bool AllowCreate { get; set; }
    }
}
