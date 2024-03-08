namespace XrEditor
{
    public class PropertyView : BaseView
    {

        public PropertyView()
        {

        }

        public string? Label { get; set; }

        public IPropertyEditor? Editor { get; set; }
    }
}
