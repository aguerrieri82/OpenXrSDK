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
        readonly Dictionary<int, DrumKitControl> _controls = [];

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
            _controls.Clear();

            if (_drumKit.Pads == null)
                return;

            foreach (var pad in _drumKit.Pads)
            {
                if (pad.Triggers != null)
                {

                    foreach (var trigger in pad.Triggers)
                    {
                        _triggers[trigger.MidiNote] = trigger;
                        _pads[trigger.MidiNote] = pad;
                    }
                }

                if (pad.Controls != null)
                {
                    foreach (var control in pad.Controls)
                    {
                        _controls[control.MidiCode] = control;
                        _pads[control.MidiCode] = pad;
                    }
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
                    Value = noteOn.Velocity / 127f,
                    Key = noteOn.Note,
                    Pad = pad.Type,
                    Trigger = trigger.Type,
                    Time = e.Timestamp,
                    Type = DrumEventType.Hit
                });
            }

            else if (msg is ControlChangeMessage cc)
            {
                if (!_controls.TryGetValue(cc.Controller, out var control))
                    return;

                DrumEvent?.Invoke(this, new DrumEvent
                {
                    Value = cc.Value / 127f,
                    Key = cc.Controller,
                    Control = control.Type,
                    Type = DrumEventType.Control
                });
            }
        }

        public bool IsOpen => _isOpen;

        public ulong RefTimeMs => _midiIn.RefTimeMs;

        public IMidiInPort Input => _midiIn;

        public DrumKit Kit => _drumKit;

        public event EventHandler<DrumEvent>? DrumEvent;
    }
}
