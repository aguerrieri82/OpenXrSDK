using XrEngine.Audio.Midi;
using XrEngine.Devices;
using XrEngine.Media;

namespace XrEngine.Music
{
    public class MidiOutSynth : ISynth
    {
        private IMidiDevice? _device;
        private IMidiOutPort? _port;

        public MidiOutSynth()
        {
            Channel = 10;
        }

        public async Task OpenAsync(string deviceId, int portNum)
        {
            Close();
            _device = Context.Require<IMidiManager>().GetDevice(deviceId);
            if (_device != null)
            {
                await _device.OpenAsync();
                _port = _device.OpenOutput(portNum);
            }
            if (_port == null)
                throw new Exception();

        }

        public void Close()
        {
            _port?.Close();
            _port = null;

            _device?.Close();
            _device = null;
        }

        public void ControlCode(int number, int value)
        {
            Send(new ControlChangeMessage()
            {
                Channel = (byte)Channel,
                Controller = (byte)number,
                Value = (byte)value
            });
        }

        public void NoteOn(int note, float velocity)
        {
            Send(new NoteOnMessage()
            {
                Channel = (byte)Channel,
                Note = (byte)note,
                Velocity = (byte)(velocity * 127)
            });
        }
        public void NoteOff(int note, float velocity)
        {
            Send(new NoteOffMessage()
            {
                Channel = (byte)Channel,
                Note = (byte)note,
                Velocity = (byte)(velocity * 127)
            });
        }

        public void AllSoundOff()
        {
            ControlCode(120, 0);
        }

        void Send(IMidiMessage msg)
        {
            var buffer = new byte[3];
            var len = msg.Encode(buffer, 0);
            _port?.Send(buffer, 0, len);
        }

        public IAudioStream? Stream => null;

        public float Volume
        {
            get => 1;
            set => throw new NotSupportedException();
        }

        public SynthCaps Caps => SynthCaps.None;

        public int Channel { get; set; }
    }
}
