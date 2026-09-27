using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace XrEngine.Music
{
    public class DrumCursor
    {
        internal int BlockIndex;
        internal int EventIndex;
        internal int Version = -1;

        public float Time { get; internal set; } = -1;
    }

    public class DrumAudioTrack : AudioTrack
    {
        protected IDrumSource? _drumSource;
        protected ISynth? _synth;
        protected readonly DrumCursor _playCursor;
        protected readonly List<DrumEvent> _buffer;
        protected int _cursorVersion;

        public DrumAudioTrack() : base(AudioTrackType.Drum)
        {
            _playCursor = new DrumCursor();
            _buffer = [];
        }

        public ISynth? Synth
        {
            get => _synth;
            set
            {
                _synth = value;
            }
        }

        public IDrumSource? Source
        {
            get => _drumSource;
            set
            {
                _drumSource = value;
            }
        }

        public bool GetNextEvents(DrumCursor cursor, List<DrumEvent> result, float lookAhead = float.PositiveInfinity)
        {
            result.Clear();

            Debug.Assert(_sequencer != null);

            var pos = _sequencer.Position;
            var maxTime = pos + lookAhead;

            if (cursor.Version != _cursorVersion)
            {
                cursor.BlockIndex = 0;
                cursor.EventIndex = 0;
                cursor.Time = -1;
                cursor.Version = _cursorVersion;
            }

            while (cursor.BlockIndex < _blocks.Count)
            {
                if (_blocks[cursor.BlockIndex] is not DrumAudioBlock drumBlock)
                {
                    cursor.BlockIndex++;
                    cursor.EventIndex = 0;
                    continue;
                }

                while (cursor.EventIndex < drumBlock.Events.Count)
                {
                    var ev = drumBlock.Events[cursor.EventIndex];
                    var absTime = drumBlock.Start + ev.Time;

                    if (absTime < pos)
                    {
                        cursor.EventIndex++;
                        continue;
                    }

                    if (absTime > maxTime)
                        return false;

                    cursor.Time = absTime;
                    var startTime = ev.Time;

                    while (cursor.EventIndex < drumBlock.Events.Count)
                    {
                        ev = drumBlock.Events[cursor.EventIndex];

                        if (Math.Abs(ev.Time - startTime) > 0.000001f)
                            break;

                        result.Add(ev);
                        cursor.EventIndex++;
                    }

                    return true;
                }

                cursor.BlockIndex++;
                cursor.EventIndex = 0;
            }

            cursor.Time = -1;
            return false;
        }

        internal override void SetPosition(float value)
        {
            _activeBlock = null;
            _cursorVersion++;

            if (_state == AudioTrackState.Play)
                _synth?.AllSoundOff();

            base.SetPosition(value);
        }

        protected override void PlayWork()
        {
            if (!IsAudible)
            {
                Thread.Sleep(2);
                return;
            }
            
            Debug.Assert(_sequencer != null);

            if (!GetNextEvents(_playCursor, _buffer))
            {
                Stop();
                return;
            }

            var wait = (_playCursor.Time - _sequencer.Position) / _sequencer.TimeScale;

            foreach (var ev in _buffer)
                _sequencer.OnEvent(this, ev, wait);

            if (wait > 0)
                EngineNativeLib.SleepFor((ulong)(wait * 1000000000));

            foreach (var ev in _buffer)
            {
                if (ev.Type == DrumEventType.Hit)
                    _synth?.NoteOn(ev.MidiNote, ev.Force);

                else if (ev.Type == DrumEventType.Control)
                    _synth?.ControlCode(ev.MidiNote, (int)(ev.Force * 127));
            }
        }

        protected override void StartRecord()
        {
            if (_drumSource == null)
                throw new InvalidOperationException();

            var block = new DrumAudioBlock();
            block.Start = _sequencer!.Position;

            _blocks.Add(block);
            _activeBlock = block;

            block.StartRecord();
        }

        protected override void StopRecord()
        {
            if (_activeBlock is not DrumAudioBlock drumBlock)
                throw new InvalidOperationException();

            drumBlock.StopRecord();

            _activeBlock = null;

            base.StopRecord();
        }

        public static DrumAudioTrack LoadSequence(DrumSequence sequence)
        {
            var track = new DrumAudioTrack();

            track.AddBlock(new DrumAudioBlock(sequence.Events ?? []));

            return track;
        }

        public static DrumAudioTrack LoadSequence(string path)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                IncludeFields = true
            };

            var json = File.ReadAllText(path);
            var seq = JsonSerializer.Deserialize<DrumSequence>(json, options);

            Debug.Assert(seq?.Events != null);

            var track = new DrumAudioTrack();
            var block = new DrumAudioBlock(seq?.Events ?? []);

            track.AddBlock(block);

            return track;
        }

        public IList<DrumEvent> GetEvents()
        {
            var result = new List<DrumEvent>();

            foreach (var block in _blocks)
            {
                if (block is not DrumAudioBlock drumBlock)
                    continue;

                foreach (var ev in drumBlock.Events)
                {
                    var item = ev.Clone();
                    item.Time += drumBlock.Start;
                    result.Add(item);
                }
            }

            return result;
        }
    }
}