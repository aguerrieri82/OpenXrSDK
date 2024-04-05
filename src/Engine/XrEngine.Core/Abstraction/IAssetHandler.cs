namespace XrEngine
{
    public interface IAssetHandler
    {
        bool CanHandle(Uri uri, out Type resType);

        EngineObject LoadAsset(Uri uri, Type resType, IAssetManager assetManager, object? options = null);


    }
}
