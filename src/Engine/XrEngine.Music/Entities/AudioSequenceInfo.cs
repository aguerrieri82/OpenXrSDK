
namespace XrEngine.Music
{

    public enum PlayDifficulty
    {
        Beginner,
        Easy,
        Medium,
        Hard,
        Advanced,
        Expert
    }

    [Flags]
    public enum PracticeScoreType
    {
        None = 0,
        Timing = 1,
        Accuracy = 2,
        HandAccuracy = 4,
        Completeness = 8,
        Dynamics = 16
    }

    public class PracticeFocusInfo
    {
        public PracticeScoreType ScoreTypes { get; set; }

        public IList<DrumPadType>? Pads { get; set; }

        public IList<DrumPlayerPart>? PlayerParts { get; set; }
    }


    public enum AudioContentType
    {
        Song,
        Exercise
    }

    public enum AudioTrackInstrument
    {
        None,
        Drum,
        Voice
    }

    public enum AudioTrackType
    {
        Wave,
        Midi,
        Drum
    }

    public struct TimeSignature
    {
        public int Den;

        public int Num;
    }


    public class AudioTrackInfo
    {
        public Guid Id { get; set; }

        public AudioTrackType Type { get; set; }

        public AudioTrackInstrument Instrument { get; set; }

        public string? ContentUri { get; set; }

        public string? MimeType { get; set; }

        public object? Content { get; set; }
    }

    public class AudioSectionPracticeInfo
    {
        public bool Loop { get; set; }

        public bool Score { get; set; }

        public float? MinScore { get; set; }

        public IList<AudioTragetInfo>? Targets { get; set; }

        public PracticeFocusInfo? Focus { get; set; }
    }

    public class AudioTragetInfo
    {
        public float Offset { get; set; }

        public float Duration { get; set; }
    }


    public class AudioSectionInfo
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public float Offset { get; set; }

        public float Duration { get; set; }

        public AudioSectionPracticeInfo? Practice { get; set; }

    }

    public class AudioSequenceInfo
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Category { get; set; }

        public PlayDifficulty Difficulty { get; set; }

        public AudioContentType Type { get; set; }

        public float Bpm { get; set; }

        public TimeSignature Tempo { get; set; }

        public IList<AudioTrackInfo>? Tracks { get; set; }

        public IList<AudioSectionInfo>? Sections { get; set; }

    }
}
