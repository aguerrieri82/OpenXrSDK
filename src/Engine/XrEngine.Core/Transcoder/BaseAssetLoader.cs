namespace XrEngine.Transcoder
{
    public abstract class BaseAssetLoader : IAssetHandler
    {
        protected abstract bool CanHandleExtension(string extension);

        protected string GetFilePath(Uri uri)
        {
            return uri.IsAbsoluteUri ? uri.LocalPath : uri.ToString();
        }

        public bool CanHandle(Uri uri, out Type resType)
        {
            if (CanHandleExtension(Path.GetExtension(GetFilePath(uri)).ToLower()))
            {
                resType = typeof(Texture);
                return true;
            }
            resType = typeof(object);
            return false;
        }

        public abstract EngineObject LoadAsset(Uri uri, Type resType, IAssetManager assetManager, EngineObject? destObj, IAssetLoaderOptions? options = null);
    }
}
