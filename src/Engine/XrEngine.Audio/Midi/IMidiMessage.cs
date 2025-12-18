namespace XrEngine.Audio.Midi
{
    public interface IMidiMessage
    {
        int Encode(Span<byte> buffer, int offset);
    }
}
