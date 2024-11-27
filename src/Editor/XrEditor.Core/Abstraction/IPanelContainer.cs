namespace XrEditor
{
    public interface IPanelContainer
    {
        void Remove(IPanel panel);

        IPanel? ActivePanel { get; set; }
    }
}
