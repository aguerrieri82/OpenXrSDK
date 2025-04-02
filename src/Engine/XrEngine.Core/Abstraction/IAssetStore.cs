namespace XrEngine
{
    public interface IAssetStore
    {
        Stream Open(string name);

        bool Contains(string name);

        string GetPath(string name);

        IEnumerable<string> List(string storePath);

        IEnumerable<string> ListDirectories(string storePath);
    }
}
