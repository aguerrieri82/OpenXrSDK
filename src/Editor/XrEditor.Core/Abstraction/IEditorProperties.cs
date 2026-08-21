namespace XrEditor
{
    public enum PropertiesGenerationMode
    {
        None = 0,
        OnlySelf = 1,

        All = 2
    }

    public interface IEditorProperties
    {
        void EditorProperties(IList<PropertyView> curProps);

        public PropertiesGenerationMode AutoGenerate { get; set; }
    }
}
