using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


    public interface IVideoCodec :IDisposable
    {
        void Open(VideoCodecMode mode, string mimeType, VideoFormat outFormat);

        bool Convert(FrameBuffer src, ref FrameBuffer dst);

        Texture2D? OutTexture { get; set; }

        VideoCodecCaps Caps { get; }    
    }
}
