
using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Media
{ 
    public interface IAudioDecoder
    {
        byte[] DecodeToPCM(string path, out AudioFormat format);
    }
}
