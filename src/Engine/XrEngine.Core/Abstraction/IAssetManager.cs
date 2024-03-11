namespace XrEngine
{
    public interface IAssetManager
    {
        Stream Open(string name);

        string GetFsPath(string name);

        IEnumerable<string> List(string path);
    }
}
