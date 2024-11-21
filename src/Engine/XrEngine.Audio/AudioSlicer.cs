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

            var rangeX = Function.RangeX;
   
            var xt = (value - MinValue) / (MaxValue - MinValue);

            if (Function == null)
                return Data.Duration() * xt;

            var curX = rangeX.Min + xt * (rangeX.Size);    

            var curY = Function.Value(curX);

            var timeT =  (curY - StartTime) / (EndTime - StartTime);

            return Data.Duration() * timeT;
        }

        public unsafe int BestLoopOffset(float time, float length)
        {
            Debug.Assert(Data != null);

            _floatBuffer ??= Data.ToFloat();

            var minSamples = 0;
            var maxSamples = (int)(Data.Format!.SampleRate * length) / 2;

            fixed (float* pFloat = _floatBuffer)
            {
                var startSample = (int)(Data.Format!.SampleRate * time);
                var endSample = (int)(Data.Format!.SampleRate * (time + length));


                var bestOfs = -1;
                float bestValue = float.NegativeInfinity;

                var len = (endSample - startSample + 1);

                for (int ofs = minSamples; ofs <= maxSamples; ofs++)
                {
                    float sum = 0;
                    for (int j = 0; j < len; j++)
                    {
                        var s1 = endSample - ofs + j;
                        var s2 = startSample + j;

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

            var time = TimeForValue(value);
            var key = $"{value}|{length}";
            if (!OffsetMap.TryGetValue(key, out var ofs))
            {
                ofs = BestLoopOffset(time, length);
                OffsetMap[key] = ofs;
            }

            var startSample = Data.Format.TimeToSample(time) * (Data.Format.BitsPerSample / 8);
            var endSample = (Data.Format.TimeToSample(time + length) - ofs) * (Data.Format.BitsPerSample / 8);

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
