namespace XrEngine.Music
{
    public static class DrumKitSwitches
    {
        public const string VariableHiHatOn = nameof(VariableHiHatOn);

        public const string VariableHiHatOff = nameof(VariableHiHatOff);
    }

    public class DrumKitSwitch
    {
        public int Key { get; set; }

        public string? Name { get; set; }    
    }
}
