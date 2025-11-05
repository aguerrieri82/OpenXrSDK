namespace XrEngine.Audio.Midi
{

    public struct ControlChangeMessage : IMidiMessage
    {
        public byte Channel;
        public byte Controller;
        public byte Value;

        public const int Length = 3;

        public static ControlChangeMessage? Decode(ReadOnlySpan<byte> data)
        {
            if (data.Length < Length || (data[0] & 0xF0) != 0xB0)
                return null;

            return new ControlChangeMessage
            {
                Channel = (byte)(data[0] & 0x0F),
                Controller = data[1],
                Value = data[2]
            };
        }

        public int Encode(Span<byte> buffer, int offset)
        {
            if (offset + Length > buffer.Length)
                throw new ArgumentException("Buffer too small.");

            buffer[offset] = (byte)(0xB0 | (Channel & 0x0F));
            buffer[offset + 1] = Controller;
            buffer[offset + 2] = Value;
            return Length;
        }

        public override string ToString() => $"CC      Ch:{Channel + 1} Ctrl:{Controller} Val:{Value}";
    }
}
