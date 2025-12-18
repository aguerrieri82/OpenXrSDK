using OpenAl.Framework;
using System.Diagnostics;
using XrMath;

namespace XrEngine.Audio
{
    public class AudioSlicer
    {
        float[]? _floatBuffer;

        public AudioSlicer()
        {
            OffsetMap = [];
        }

        public float TimeForValue(float value)
        {
            Debug.Assert(Data != null);
            Debug.Assert(Function != null);

            Bounds1 rangeX = Function.RangeX;

            float xt = (value - MinValue) / (MaxValue - MinValue);

            if (Function == null)
                return Data.Duration() * xt;

            float curX = rangeX.Min + xt * (rangeX.Size);

            float curY = Function.Value(curX);

            float timeT = (curY - StartTime) / (EndTime - StartTime);

            return Data.Duration() * timeT;
        }

        public unsafe int BestLoopOffset(float time, float length)
        {
            Debug.Assert(Data != null);

            _floatBuffer ??= Data.ToFloat();

            int minSamples = 0;
            int maxSamples = (int)(Data.Format!.SampleRate * length) / 2;

            fixed (float* pFloat = _floatBuffer)
            {
                int startSample = (int)(Data.Format!.SampleRate * time);
                int endSample = (int)(Data.Format!.SampleRate * (time + length));


                int bestOfs = -1;
                float bestValue = float.NegativeInfinity;

                int len = (endSample - startSample + 1);

                for (int ofs = minSamples; ofs <= maxSamples; ofs++)
                {
                    float sum = 0;
                    for (int j = 0; j < len; j++)
                    {
                        int s1 = endSample - ofs + j;
                        int s2 = startSample + j;

                        sum += pFloat[s1] * pFloat[s2];
                    }

                    if (sum > bestValue)
                    {
                        bestValue = sum;
                        bestOfs = ofs;
                    }
                }

                return bestOfs;
            }
        }

        public void FillBuffer(float value, float length, ref byte[] buffer)
        {
            Debug.Assert(Data?.Buffer != null);
            Debug.Assert(Data?.Format != null);

            float time = TimeForValue(value);
            string key = $"{value}|{length}";
            if (!OffsetMap.TryGetValue(key, out int ofs))
            {
                ofs = BestLoopOffset(time, length);
                OffsetMap[key] = ofs;
            }

            int startSample = Data.Format.TimeToSample(time) * (Data.Format.BitsPerSample / 8);
            int endSample = (Data.Format.TimeToSample(time + length) - ofs) * (Data.Format.BitsPerSample / 8);

            Array.Resize(ref buffer, endSample - startSample);

            Array.Copy(Data.Buffer, startSample, buffer, 0, buffer.Length);
        }

        public Dictionary<string, int> OffsetMap { get; set; }

        public AudioData? Data { get; set; }

        public IFunction2? Function { get; set; }

        public float StartTime { get; set; }

        public float EndTime { get; set; }

        public float MinValue { get; set; }

        public float MaxValue { get; set; }

    }
}
