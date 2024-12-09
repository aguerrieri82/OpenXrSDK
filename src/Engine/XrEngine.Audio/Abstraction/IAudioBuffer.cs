using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Audio
{
    public interface IAudioBuffer
    {
        void CopyTo(Span<byte> buffer, int offset, int startSample, int sampleCount);
    }
}
