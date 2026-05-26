namespace XrEngine.Audio.Midi
{
    public struct NoteOffMessage : IMidiMessage
    {
        public byte Channel;
        public byte Note;
        public byte Velocity;

        public const int Length = 3;

        public static NoteOffMessage? Decode(ReadOnlySpan<byte> data)
        {
            if (data.Length < Length || (data[0] & 0xF0) != 0x80)
                return null;

            return new NoteOffMessage
            {
                Channel = (byte)(data[0] & 0x0F),
                Note = data[1],
                Velocity = data[2]
            };
        }

        public int Encode(Span<byte> buffer, int offset)
        {
            if (offset + Length > buffer.Length)
                throw new ArgumentException("Buffer too small.");

            buffer[offset] = (byte)(0x80 | (Channel & 0x0F));
            buffer[offset + 1] = Note;
            buffer[offset + 2] = Velocity;
            return Length;
        }

        public override string ToString() => $"NoteOff Ch:{Channel + 1} Note:{Note} Vel:{Velocity}";
    }
}

