using XrEngine.Media;

namespace XrEngine.Music
{
    [Flags]
    public enum SynthCaps
    {
        None = 0x1,
        Volume = 0x2,
        Streamable = 0x4,
    }

    public interface ISynth
    {
        void ControlCode(int number, int value);

        void NoteOn(int note, float velocity);

        void NoteOff(int note, float velocity);

        void AllSoundOff();

        IAudioStream? Stream { get; }

        float Volume { get; set; }

        SynthCaps Caps { get; }
    }
}
