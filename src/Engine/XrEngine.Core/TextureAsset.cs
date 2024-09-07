using XrEngine.Services;

namespace XrEngine
{
    public class TextureAsset : BaseAsset<TextureLoadOptions, BaseTextureLoader>
    {
        public TextureAsset(BaseTextureLoader loader, string filePath, TextureLoadOptions? options)
            : base(loader, Path.GetFileName(filePath), typeof(Texture2D), new Uri(filePath), options)
        {

        }

        public TextureAsset(BaseTextureLoader loader, Uri uri, TextureLoadOptions? options)
            : base(loader, uri.LocalPath, typeof(Texture2D), uri, options)
        {

        }

        public TextureAsset FromFile(string filePath, TextureLoadOptions? options = null)
        {
            var loader = (BaseTextureLoader)AssetLoader.Instance.GetLoader(new Uri(filePath));
            return new TextureAsset(loader, filePath, options);
        }
    }
}
