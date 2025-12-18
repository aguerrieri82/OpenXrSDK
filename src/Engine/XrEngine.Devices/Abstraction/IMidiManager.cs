namespace XrEngine.Devices
{
    public class MidiDeviceInfo
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public int InputPortCount { get; set; }

        public int OutputPortCount { get; set; }
    }

    public interface IMidiManager
    {
        IList<MidiDeviceInfo> FindDevices();

        IMidiDevice? GetDevice(string id);
    }
}
