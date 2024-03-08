using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Enumeration;

namespace Xr.Engine.Audio
{
    public interface IAudioDecoder
    {
        AudioData Decode(Stream stream);
    }
}
