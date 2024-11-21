using OpenAl.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Audio
{
    public interface IAudioStream
    {
        void Start();

        void Stop();

        int Fill(byte[] data, float timeSec);

        int PrefBufferSize { get; }

        int PrefBufferCount { get; }

        float Length { get; }

        AudioFormat Format { get; } 

        bool IsStreaming { get; }
    }
}
