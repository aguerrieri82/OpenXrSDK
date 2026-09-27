using XrEngine.Audio.Midi;
using XrEngine.Devices;

namespace XrEngine.Music
{
    public class MidiInDrumSource : IDrumSource
    {
        readonly IMidiInPort _midiIn;
        readonly bool _isOpen;
        readonly DrumKit _drumKit;
        readonly Dictionary<int, DrumKitTrigger> _triggers = [];
        readonly Dictionary<int, DrumKitPad> _pads = [];

        public MidiInDrumSource(IMidiInPort midiIn, DrumKit kit)
        {
            _midiIn = midiIn;
            _isOpen = true;
            _drumKit = kit;
            LoadKit();
        }

        protected void LoadKit()
        {
            _triggers.Clear();
            _pads.Clear();

            if (_drumKit.Pads == null)
                return;

            foreach (var pad in _drumKit.Pads)
            {
                if (pad.Triggers == null)
                    continue;

                foreach (var trigger in pad.Triggers)
                {
                    _triggers[trigger.MidiNote] = trigger;
                    _pads[trigger.MidiNote] = pad;
                }
            }
        }

        public void Close()
        {
            _midiIn.DataReceived -= OnDataReceived;
        }

        public void Open()
        {
            _midiIn.DataReceived += OnDataReceived;
        }

        private void OnDataReceived(object? sender, MidiData e)
        {
            var span = new ReadOnlySpan<byte>(e.Data, e.Offset, e.Count);
            var msg = MidiMessageDecoder.Decode(span);

            if (msg is NoteOnMessage noteOn)
            {
                if (!_triggers.TryGetValue(noteOn.Note, out var trigger))
                    return;

                var pad = _pads[noteOn.Note];
                DrumEvent?.Invoke(this, new DrumEvent
                {
                    Force = noteOn.Velocity / 127f,
                    MidiNote = noteOn.Note,
                    Pad = pad.Type,
                    Trigger = trigger.Type,
                    Time = e.Timestamp,
                    Type = DrumEventType.Hit
                });
            }

            else if (msg is ControlChangeMessage cc)
            {
                if (!_triggers.TryGetValue(cc.Controller, out var trigger))
                    return;

                DrumEvent?.Invoke(this, new DrumEvent
                {
                    Force = cc.Value / 127f,
                    MidiNote = cc.Controller,
                    Trigger = trigger.Type,
                    Type = DrumEventType.Control
                });
            }
        }

        public bool IsOpen => _isOpen;

        public ulong RefTimeMs => _midiIn.RefTimeMs;

        public event EventHandler<DrumEvent>? DrumEvent;

    }
}
