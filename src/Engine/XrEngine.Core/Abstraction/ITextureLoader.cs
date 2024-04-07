namespace XrEngine
{
    public class TextureReadOptions : IAssetLoaderOptions
    {
        public TextureFormat? Format { get; set; }
    }


    public interface ITextureLoader
    {
        IList<TextureData> Read(Stream stream, TextureReadOptions? options = null);
    }
}
