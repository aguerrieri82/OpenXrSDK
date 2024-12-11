namespace XrEditor
{
    public interface IItemsSource
    {

        IEnumerable<object> Filter(string? query);

        string? GetText(object? item);

        object? GetValue(object? item);

    }
}
