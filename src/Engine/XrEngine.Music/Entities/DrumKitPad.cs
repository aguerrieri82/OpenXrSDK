using System.Numerics;

namespace XrEngine.Music
{
    public enum DrumPadCategory
    {
        Drum,
        Cymbal
    }

    public enum DrumPadType
    {
        Kick,
        Snare,

        HiTom,
        MidTom,
        LowTom,
        FloorTom,

        HiHat,
        Ride,
        Crash,
        China,
        Splash,

        Cowbell,
        Tamburine
    }

    public class DrumKitPad
    {
        public DrumPadType Type { get; set; }

        public DrumPadCategory Category { get; set; }

        public IList<DrumKitTrigger>? Triggers { get; set; }

        public IList<DrumKitControl>? Controls { get; set; }

        public Vector3 Position { get; set; }
    }
}