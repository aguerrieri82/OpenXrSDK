#if ANDROID23_0_OR_GREATER

using Android.Media.Midi;
using Android.Net.Wifi;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using static Android.Icu.Text.IDNA;
using MidiDeviceInfo2 = Android.Media.Midi.MidiDeviceInfo;

namespace XrEngine.Devices.Android
{
    [SupportedOSPlatform("android23.0")]
    public class AndroidMidiDevice : IMidiDevice
    {
        readonly MidiDeviceInfo2 _info;
        readonly MidiManager _manager;
        MidiDevice? _device;

        class OnDeviceOpenedListener : Java.Lang.Object, MidiManager.IOnDeviceOpenedListener
        {
            private readonly TaskCompletionSource<MidiDevice?> _source;

            public OnDeviceOpenedListener()
            {
                _source = new();

            }
            public void OnDeviceOpened(MidiDevice? device)
            {
                _source.SetResult(device);
            }

            public Task<MidiDevice?> Task => _source.Task;
        }

        public AndroidMidiDevice(MidiDeviceInfo2 info, MidiManager midiManager)
        {
            _info = info;
            _manager = midiManager;
        }

        public async Task OpenAsync()
        {
            OnDeviceOpenedListener listener = new OnDeviceOpenedListener();
            _manager.OpenDevice(_info, listener, new Handler(Looper.MainLooper!));
            _device = await listener.Task;
            if (_device == null)
                throw new Exception("Failed to open MIDI device.");
        }

        public void Close()
        {
            _device?.Close();
            _device = null;
        }

        public IMidiOutPort OpenOutput(int index)
        {
            MidiInputPort? port = _device?.OpenInputPort(index);
            if (port == null)
                throw new Exception("Failed to open MIDI output port.");
            return new AndroidMidiOutPort(port);
        }

        public IMidiInPort OpenInput(int index)
        {
            MidiOutputPort? port = _device?.OpenOutputPort(index);
            if (port == null)
                throw new Exception("Failed to open MIDI output port.");
            return new AndroidMidiInPort(port);
        }

        public int InputPortCount => _info.OutputPortCount;

        public int OutputPortCount => _info.InputPortCount;

        public string Id => _info.Id.ToString();

        //NOTE: Android's input/output ports are from the perspective of the device.    
    }
}

#endif