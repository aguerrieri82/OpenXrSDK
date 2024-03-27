namespace XrEditor
{
    public interface IItemView
    {
        string DisplayName { get; }

        IconView? Icon { get; }
    }
}
