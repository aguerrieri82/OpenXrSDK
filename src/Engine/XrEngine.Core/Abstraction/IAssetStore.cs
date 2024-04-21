namespace XrEngine
{
    public interface IAssetStore
    {
        Stream Open(string name);

        string GetPath(string name);

        IEnumerable<string> List(string storePath);
    }
}
