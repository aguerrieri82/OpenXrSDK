#if ANDROID23_0_OR_GREATER

using Android.Media.Midi;
using System.Collections.Immutable;
using System.Runtime.Versioning;
using ContextA = global::Android.Content.Context;
using MidiDeviceInfo2 = Android.Media.Midi.MidiDeviceInfo;

namespace XrEngine.Devices.Android
{
    [SupportedOSPlatform("android23.0")]
    public class AndroidMidiManager : IMidiManager
    {
        readonly MidiManager _manager;

        public AndroidMidiManager()
        {
            _manager = (MidiManager)Application.Context.GetSystemService(ContextA.MidiService)!;
        }


        public IList<MidiDeviceInfo> FindDevices()
        {
            MidiDeviceInfo2[]? devices = _manager.GetDevices();

            if (devices == null || devices.Length == 0)
                return Array.Empty<MidiDeviceInfo>();

            return devices.Select(a => new MidiDeviceInfo()
            {
                Id = a.Id.ToString(),
                Name = a.Properties?.GetString(MidiDeviceInfo2.PropertyName) ?? "Unknown",
                InputPortCount = a.InputPortCount,
                OutputPortCount = a.OutputPortCount
            }).ToImmutableArray();
        }

        public IMidiDevice? GetDevice(string id)
        {
            if (int.TryParse(id, out int deviceId))
            {
                MidiDeviceInfo2? deviceInfo = _manager.GetDevices()?.FirstOrDefault(d => d.Id == deviceId);
                if (deviceInfo != null)
                    return new AndroidMidiDevice(deviceInfo, _manager);
            }
            return null;
        }
    }
}

#endif