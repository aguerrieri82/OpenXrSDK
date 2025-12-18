namespace XrEngine.Audio.Midi
{
    public static class MidiMessageDecoder
    {
        public static IMidiMessage? Decode(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0)
                return null;

            byte status = data[0];

            return status switch
            {
                0xFE => ActiveSensingMessage.Decode(data),
                _ when (status & 0xF0) == 0x80 => NoteOffMessage.Decode(data),
                _ when (status & 0xF0) == 0x90 => NoteOnMessage.Decode(data),
                _ when (status & 0xF0) == 0xB0 => ControlChangeMessage.Decode(data),
                _ when (status & 0xF0) == 0xC0 => ProgramChangeMessage.Decode(data),
                _ => null
            };
        }
    }
}
