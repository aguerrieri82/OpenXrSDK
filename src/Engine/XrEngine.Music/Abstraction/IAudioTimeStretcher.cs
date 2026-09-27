using XrEngine.Media;

namespace XrEngine.Music
{
    public interface IAudioTimeStretcher : IDisposable
    {
        void Configure(AudioFormat format);

        void Reset();

        void SetTempo(float scale);

        void Write(ReadOnlySpan<short> input);

        void Flush();

        int Read(Span<short> output);

        bool IsDrained { get; }
    }
}
