namespace XrEngine
{
    public class EngineAssetLoader : IAssetLoader
    {
        public bool CanHandle(Uri uri, out Type resType)
        {
            throw new NotImplementedException();
        }

        public EngineObject LoadAsset(Uri uri, Type resType, EngineObject? curObj, IAssetLoaderOptions? options = null)
        {
            throw new NotImplementedException();
        }
    }
}
