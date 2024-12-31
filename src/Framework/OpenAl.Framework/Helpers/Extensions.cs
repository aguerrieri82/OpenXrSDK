using Silk.NET.OpenAL;

namespace OpenAl.Framework
{
    public static class Extensions
    {

        public static void CheckError(this AL al, string msg)
        {
            var err = al.GetError();
            if (err != AudioError.NoError)
                throw new InvalidOperationException($"{err} - {msg}");
        }


        public static float SampleToTimeByte(this AudioFormat self, int sample)
        {
            var sampleSize = (self.BitsPerSample / 8) * self.Channels;

            return (sample / sampleSize) / (float)self.SampleRate;
        }

        public static int TimeToSampleByte(this AudioFormat self, float time)
        {
            var sampleSize = (self.BitsPerSample / 8) * self.Channels;

            var result = (int)(time * self.SampleRate * sampleSize);
            var res = result % sampleSize;
            return result - res;
        }

        public static float SampleToTime(this AudioFormat self, int sample)
        {
            return sample / (float)self.SampleRate;
        }

        public static int TimeToSample(this AudioFormat self, float time)
        {
            return (int)(time * self.SampleRate);
        }

        public static float Duration(this AudioData self)
        {
            return self.Format.SampleToTimeByte(self.Buffer.Length - 1);
        }

        public unsafe static T ReadStruct<T>(this Stream stream) where T : unmanaged
        {
            var buffer = stackalloc T[1];

            var span = new Span<byte>((byte*)buffer, sizeof(T));

            stream.ReadExactly(span);

            return *buffer;
        }

        public unsafe static float[] ToFloat(this AudioData self)
        {
            var result = new float[self.Buffer.Length / (self.Format.BitsPerSample / 8)];

            fixed (float* pResult = result)
            fixed (byte* pBuf = self.Buffer)
            {
                if (self.Format.BitsPerSample == 8)
                {
                    var pByte = (sbyte*)pBuf;
                    for (int i = 0; i < result.Length; i++)
                        pResult[i] = pByte[i] / (float)sbyte.MaxValue;
                }

                else if (self.Format.BitsPerSample == 16)
                {
                    var pShort = (short*)pBuf;
                    for (int i = 0; i < result.Length; i++)
                        pResult[i] = pShort[i] / (float)short.MaxValue;
                }
                else
                    throw new NotSupportedException();
            }

            return result;
        }

    }
}
