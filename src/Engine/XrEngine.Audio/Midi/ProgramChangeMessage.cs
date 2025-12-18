namespace XrEngine.Audio.Midi
{
    public struct ProgramChangeMessage : IMidiMessage
    {
        public byte Channel;
        public byte Program;

        public const int Length = 2;

        public static ProgramChangeMessage? Decode(ReadOnlySpan<byte> data)
        {
            if (data.Length < Length || (data[0] & 0xF0) != 0xC0)
                return null;

            return new ProgramChangeMessage
            {
                Channel = (byte)(data[0] & 0x0F),
                Program = data[1]
            };
        }

        public int Encode(Span<byte> buffer, int offset)
        {
            if (offset + Length > buffer.Length)
                throw new ArgumentException("Buffer too small.");

            buffer[offset] = (byte)(0xC0 | (Channel & 0x0F));
            buffer[offset + 1] = Program;
            return Length;
        }

        public override string ToString() => $"Program Ch:{Channel + 1} Prog:{Program}";
    }
}

