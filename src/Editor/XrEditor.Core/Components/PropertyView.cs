namespace XrEditor
{
    public class PropertyView : BaseView
    {

        public PropertyView()
        {

        }

        public string? Label { get; set; }

        public string? Category { get; set; }

        public bool ReadOnly { get; set; }

        public IPropertyEditor? Editor { get; set; }


    }
}
