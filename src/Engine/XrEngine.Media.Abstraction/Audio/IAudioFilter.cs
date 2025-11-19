namespace XrEngine.Media
{
    public interface IAudioFilter
    {
        void Initialize(int inputLen, int sampleRate);

        void Transform(Span<float> input, Span<float> output);
    }
}
