using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Audio
{
    public interface IAudioFilter
    {
        void Initialize(int inputLen, int sampleRate);

        void Transform(float[] input, float[] output);
    }
}
