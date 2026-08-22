namespace XrEngine
{

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class EditableAttribute : Attribute
    {
        public EditableAttribute(bool isEditable = true)
        {
            IsEditable = isEditable;
        }

        public bool AllowCreate { get; set; }

        public bool IsEditable { get; set; }
    }
}
