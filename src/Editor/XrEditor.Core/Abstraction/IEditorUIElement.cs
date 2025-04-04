namespace XrEditor
{
    public interface IEditorUIElement
    {
        void ScrollToView();
    }

    public interface IEditorUIContainer : IEditorUIElement
    {
        void ScrollToView(object item);

        void BeginUpdate();

        void EndUpdate();
    }
}
