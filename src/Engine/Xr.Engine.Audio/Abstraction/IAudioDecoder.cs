namespace Xr.Engine.Audio
{
    public interface IAudioDecoder
    {
        AudioData Decode(Stream stream);
    }
}
