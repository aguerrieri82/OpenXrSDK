namespace OpenXr.Engine
{

    public interface ITextureReader
    {
        TextureData Read(Stream stream);
    }
}
