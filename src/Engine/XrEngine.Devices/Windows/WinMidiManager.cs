using System.Runtime.InteropServices;

namespace XrEngine.Devices.Windows
{
    public class WinMidiManager : IMidiManager
    {
        public IList<MidiDeviceInfo> FindDevices()
        {
            List<MidiDeviceInfo> list = new List<MidiDeviceInfo>();

            int outCount = (int)Win32.midiOutGetNumDevs();
            for (uint i = 0; i < (uint)outCount; ++i)
            {
                Win32.midiOutGetDevCaps(i, out Win32.MidiOutCaps caps, (uint)Marshal.SizeOf<Win32.MidiOutCaps>());

                list.Add(new MidiDeviceInfo
                {
                    Id = $"out:{i}",
                    Name = string.IsNullOrEmpty(caps.szPname) ? $"MIDI Out {i}" : caps.szPname,
                    OutputPortCount = 1
                });
            }

            int inCount = (int)Win32.midiInGetNumDevs();
            for (uint i = 0; i < (uint)inCount; ++i)
            {
                Win32.midiInGetDevCaps(i, out Win32.MidiInCaps caps, (uint)Marshal.SizeOf<Win32.MidiInCaps>());

                list.Add(new MidiDeviceInfo
                {
                    Id = $"in:{i}",
                    Name = string.IsNullOrEmpty(caps.szPname) ? $"MIDI In {i}" : caps.szPname,
                    InputPortCount = 1
                });
            }

            return list;
        }

        public IMidiDevice? GetDevice(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            string[] parts = id.Split(':', 2);
            if (parts.Length != 2)
                return null;

            if (!uint.TryParse(parts[1], out uint index))
                return null;

            string kind = parts[0].ToLowerInvariant();

            if (kind == "out")
                return new WinMidiDevice(isOutput: true, index, id);
            if (kind == "in")
                return new WinMidiDevice(isOutput: false, index, id);

            return null;
        }
    }
}
