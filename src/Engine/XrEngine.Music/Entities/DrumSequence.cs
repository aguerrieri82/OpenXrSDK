namespace XrEngine.Music
{
    public class DrumSequence
    {
        public float Bpm { get; set; }

        public TimeSignature Tempo { get; set; }

        public IList<DrumEvent>? Events { get; set; }

        public bool PartsAssigned { get; set; }  
    }
}
