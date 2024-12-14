namespace XrEngine
{
    public interface IAssetLoaderOptions
    {
    }

    public interface IAssetLoader
    {
        bool CanHandle(Uri uri, out Type assetType);

        EngineObject LoadAsset(Uri uri, Type assetType, EngineObject? destObj, IAssetLoaderOptions? options = null);
    }
}
