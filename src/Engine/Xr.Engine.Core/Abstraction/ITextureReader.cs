namespace Xr.Engine
{

    public interface ITextureReader
    {
        IList<TextureData> Read(Stream stream);
    }
}
