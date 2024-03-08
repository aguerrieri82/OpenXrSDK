namespace XrEngine
{
    public interface IAssetManager
    {
        Stream OpenAsset(string name);

        string FullPath(string name);
    }
}
