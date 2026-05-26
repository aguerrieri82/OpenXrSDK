#if __ANDROID__

using Android.Media.Midi;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;

namespace XrEngine.Devices.Android
{
    [SupportedOSPlatform("android23.0")]
    public class AndroidMidiOutPort : IMidiOutPort
    {
        readonly MidiInputPort _port;

        public AndroidMidiOutPort(MidiInputPort port)
        {
            _port = port;
        }

        public void Send(byte[] data, int offset, int count)
        {
            _port.Send(data, offset, count);
        }

        public void Close()
        {
            _port.Close();
        }
    }
}

#endif