namespace XrEngine.Audio.Midi
{
    public struct ActiveSensingMessage : IMidiMessage
    {
        public const int Length = 1;

        public static ActiveSensingMessage Decode(ReadOnlySpan<byte> data)
        {
            return new ActiveSensingMessage();
        }

        public int Encode(Span<byte> buffer, int offset)
        {
            if (offset + Length > buffer.Length)
                throw new ArgumentException("Buffer too small.");
            buffer[offset] = 0xFE;
            return Length;
        }

        public override string ToString() => "ActiveSensing";
    }
}
