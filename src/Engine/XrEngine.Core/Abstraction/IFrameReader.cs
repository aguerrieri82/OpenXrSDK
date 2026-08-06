namespace XrEngine
{
    public interface IFrameReader
    {
        TextureData ReadFrame(TextureFormat format = TextureFormat.Rgba8);
    }
}
