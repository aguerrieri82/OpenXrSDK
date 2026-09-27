
namespace XrEngine.Music
{
    public class DrumAudioBlock : BaseAudioBlock
    {
        readonly List<DrumEvent> _events = [];
        ulong _startTime;

        public DrumAudioBlock()
        {
            _events = [];
        }

        public DrumAudioBlock(IEnumerable<DrumEvent> events)
        {
            _events = [.. events];

            if (_events.Any())
            {
                var maxTime = _events.Max(a => a.Time);
                _duration = maxTime;
            }
        }

        public void StartRecord()
        {
            var source = ((DrumAudioTrack?)_track)?.Source;

            if (source == null)
                return;

            _events.Clear();
            _startTime = source.RefTimeMs;

            source?.DrumEvent += OnDrumEvent;
        }

        public void StopRecord()
        {
            var source = ((DrumAudioTrack?)_track)?.Source;

            source?.DrumEvent -= OnDrumEvent;
        }

        private void OnDrumEvent(object? sender, DrumEvent e)
        {
            var newEvent = e.Clone();

            newEvent.Time = (((IDrumSource?)sender)!.RefTimeMs - _startTime) / 1000.0f;

            _events.Add(newEvent);

            _duration = (newEvent.Time - Start);
        }

        public IList<DrumEvent> Events => _events;

    }
}
