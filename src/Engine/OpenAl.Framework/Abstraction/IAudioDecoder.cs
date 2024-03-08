namespace OpenAl.Framework
{
    public interface IAudioDecoder
    {
        AudioData Decode(Stream stream);
    }
}
