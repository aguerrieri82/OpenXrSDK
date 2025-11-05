#if ANDROID23_0_OR_GREATER

using Android.Bluetooth;
using Android.Content;
using Android.Media.Midi;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Versioning;
using System.Text;
using XrEngine.Devices;
using MidiDeviceInfo2 = Android.Media.Midi.MidiDeviceInfo;


namespace XrEngine.Devices.Android
{
    [SupportedOSPlatform("android23.0")]
    public class AndroidMidiManager : IMidiManager
    {
        MidiManager _manager;

        public AndroidMidiManager()
        {
            _manager = (MidiManager)Application.Context.GetSystemService(Context.MidiService)!;
        }


        public IList<MidiDeviceInfo> FindDevices()
        {
            var devices = _manager.GetDevices();
            
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
                var deviceInfo = _manager.GetDevices()?.FirstOrDefault(d => d.Id == deviceId);   
                if (deviceInfo != null)
                    return new AndroidMidiDevice(deviceInfo, _manager);
            }
            return null;
        }
    }
}

#endif