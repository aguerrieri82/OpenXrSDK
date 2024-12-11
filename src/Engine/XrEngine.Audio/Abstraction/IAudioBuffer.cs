namespace XrEngine.Audio
{
    public interface IAudioBuffer
    {
        void CopyTo(Span<byte> buffer, int offset, int startSample, int sampleCount);
    }
}
