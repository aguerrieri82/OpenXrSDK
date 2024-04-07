namespace XrEngine
{
    public interface IAssetLoaderOptions { }

    public interface IAssetHandler
    {
        bool CanHandle(Uri uri, out Type resType);

        EngineObject LoadAsset(Uri uri, Type resType, IAssetManager assetManager, EngineObject? curObj, IAssetLoaderOptions? options = null);


    }
}
