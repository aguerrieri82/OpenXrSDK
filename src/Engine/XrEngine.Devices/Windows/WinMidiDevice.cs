namespace XrEngine.Devices.Windows
{
    class WinMidiDevice : IMidiDevice
    {
        readonly uint _deviceIndex;
        readonly bool _isOutput;
        readonly string _id;

        public WinMidiDevice(bool isOutput, uint deviceIndex, string id)
        {
            _isOutput = isOutput;
            _deviceIndex = deviceIndex;
            _id = id;
        }

        public int InputPortCount => _isOutput ? 0 : 1;

        public int OutputPortCount => _isOutput ? 1 : 0;

        public string Id => _id;

        public Task OpenAsync()
        {
            return Task.CompletedTask;
        }

        public void Close()
        {
        }

        public IMidiOutPort OpenOutput(int index)
        {
            if (!_isOutput)
                throw new InvalidOperationException("Device has no output ports.");

            if (index != 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            return new WinMidiOutPort(_deviceIndex);
        }

        public IMidiInPort OpenInput(int index)
        {
            if (_isOutput)
                throw new InvalidOperationException("Device has no input ports.");

            if (index != 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            return new WinMidiInPort(_deviceIndex);
        }
    }
}
