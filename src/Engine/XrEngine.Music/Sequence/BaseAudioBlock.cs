namespace XrEngine.Music
{
    public abstract class BaseAudioBlock
    {
        protected AudioTrack? _track;

        protected float _duration;

        protected internal void Attach(AudioTrack audioTrack)
        {
            _track = audioTrack;
        }

        public AudioTrack? Track => _track;

        public float Start { get; set; }

        public float End => Start + _duration;

        public float Duration => _duration;
    }
}
