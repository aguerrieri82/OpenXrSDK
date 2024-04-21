namespace XrEngine
{
    public interface IAssetLoaderOptions { }

    public interface IAssetLoader
    {
        bool CanHandle(Uri uri, out Type resType);

        EngineObject LoadAsset(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null);
    }
}
