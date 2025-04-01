namespace XrEngine
{
    public class TextureLoadOptions : IAssetLoaderOptions
    {
        public TextureFormat? Format { get; set; }

        public string? MimeType { get; set; }

        public bool UseCache { get; set; } = true;
    }


    public interface ITextureLoader
    {
        IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null);
    }
}
