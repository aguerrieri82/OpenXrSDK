namespace XrEngine.Music
{

    public class DrumKit
    {
        public string? Name { get; set; }

        public IList<DrumKitPad>? Pads { get; set; }

        public IList<DrumKitSwitch>? Switches { get; set; }
    }
}
