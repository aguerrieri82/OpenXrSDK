using XrEngine.Video.Abstraction;

namespace XrEngine.Video
{
    public enum VideoCodecMode
    {
        Encode,
        Decode
    }

    [Flags]
    public enum VideoCodecCaps
    {
        None = 0,
        DecodeTexture = 0x1
    }


    public interface IVideoCodec : IDisposable
    {
        void Open(VideoCodecMode mode, string mimeType, VideoFormat outFormat, byte[]? extraData = null);

        bool EnqueueBuffer(FrameBuffer src);

        bool DequeueBuffer(ref FrameBuffer dst);

        Texture2D? OutTexture { get; set; }

        VideoCodecCaps Caps { get; }
    }
}
