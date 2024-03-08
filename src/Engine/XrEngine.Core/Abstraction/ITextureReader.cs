namespace XrEngine
{

    public interface ITextureReader
    {
        IList<TextureData> Read(Stream stream);
    }
}
