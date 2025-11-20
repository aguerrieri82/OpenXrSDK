namespace OpenAl.Framework
{
    public interface IAlAudioDecoder
    {
        AudioData Decode(Stream stream);
    }
}
