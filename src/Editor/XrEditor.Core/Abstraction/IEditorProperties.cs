namespace XrEditor
{
    public interface IEditorProperties
    {
        void EditorProperties(IList<PropertyView> curProps);

        public bool AutoGenerate { get; set; }
    }
}
