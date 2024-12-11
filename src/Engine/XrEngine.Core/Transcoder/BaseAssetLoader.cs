namespace XrEngine.Transcoder
{
    public abstract class BaseAssetLoader : IAssetLoader
    {
        protected virtual bool CanHandleExtension(string extension, out Type resType)
        {
            resType = typeof(object);
            return false;
        }

        protected string GetFilePath(Uri uri)
        {
            if (uri.Scheme == "res" && uri.Host == "asset")
                return Context.Require<IAssetStore>().GetPath(uri.LocalPath);
            return uri.IsAbsoluteUri ? uri.LocalPath : uri.ToString();
        }

        public virtual bool CanHandle(Uri uri, out Type resType)
        {
            if (CanHandleExtension(Path.GetExtension(GetFilePath(uri)).ToLower(), out resType))
                return true;
            resType = typeof(object);
            return false;
        }


        public abstract EngineObject LoadAsset(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null);


    }
}
