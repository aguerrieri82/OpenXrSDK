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

    public interface IVideoCodec :IDisposable
    {
        void Open(VideoCodecMode mode, string mimeType, VideoFormat outFormat);

        bool Convert(FrameBuffer src, ref FrameBuffer dst); 
    }
}
