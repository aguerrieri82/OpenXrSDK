namespace XrEngine.Music
{
    public enum HiHatState
    {
        Open,
        Closed,
        Pedal
    }

    public enum DrumTriggerType
    {
        Head,
        Rim,
        Edge,
        Bell,
        Pedal,
        Choke
    }

    public class DrumKitTrigger
    {
        public DrumTriggerType Type { get; set; }

        public HiHatState? HiHatState { get; set; }

        public int MidiNote { get; set; }
    }
}