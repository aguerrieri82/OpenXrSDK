namespace OpenAl.Framework
{
    public interface IAlAudioDecoder
    {
        AlAudioData Decode(Stream stream);
    }
}
