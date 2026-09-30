namespace XrEngine.Music
{
    public enum DrumEventType
    {
        Hit,
        Control
    }

    public enum DrumPlayerPart
    {
        HandL,
        HandR,
        FootL,
        FootR,
    }

    public class DrumEvent
    {
        public DrumEventType Type { get; set; }

        public float Time { get; set; }

        public int Key { get; set; }

        public float Value { get; set; }

        public DrumTriggerType? Trigger { get; set; }

        public DrumControlType? Control { get; set; }

        public DrumPadType? Pad { get; set; }

        public HiHatState? HiHat { get; set; }

        public DrumPlayerPart? PlayerPart { get; set; }

        public bool RelaxAfter { get; set; }

        public DrumEvent Clone()
        {
            return (DrumEvent)MemberwiseClone();
        }
    }
}
