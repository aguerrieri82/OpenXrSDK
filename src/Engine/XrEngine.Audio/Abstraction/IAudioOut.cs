using OpenAl.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Audio
{
    public interface IAudioOut
    {
        void Open(AudioFormat format);

        void Close();

        void Reset();

        void Enqueue(byte[] buffer);

        byte[]? Dequeue(int timeoutMs);

    }
}
