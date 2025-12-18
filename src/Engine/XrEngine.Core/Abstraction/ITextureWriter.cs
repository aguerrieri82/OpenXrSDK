namespace XrEngine
{
    public interface ITextureWriter
    {
        void SaveTexture(Stream stream, IList<TextureData> images);
    }
}
