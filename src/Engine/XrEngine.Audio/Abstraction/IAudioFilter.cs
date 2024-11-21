namespace XrEngine.Audio
{
    public interface IAudioFilter
    {
        void Initialize(int inputLen, int sampleRate);

        void Transform(float[] input, float[] output);
    }
}
