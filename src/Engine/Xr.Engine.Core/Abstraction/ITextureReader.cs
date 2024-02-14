namespace OpenXr.Engine
{

    public interface ITextureReader
    {
        IList<TextureData> Read(Stream stream);
    }
}
