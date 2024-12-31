namespace XrEngine
{
    public interface IContentProvider
    {
        bool CanHandle(Uri uri);

        Stream Open(Uri uri);
    }
}
