

namespace XrEngine.Music
{
    public interface IDrumSource
    {
        void Open();

        void Close();

        bool IsOpen { get; }

        event EventHandler<DrumEvent> DrumEvent;

        ulong RefTimeMs { get; }
    }
}
