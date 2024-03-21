namespace XrEngine
{
    public class TextureReadOptions
    {
        public TextureFormat? Format { get; set; }
    }


    public interface ITextureReader
    {
        IList<TextureData> Read(Stream stream, TextureReadOptions? options = null);
    }
}
