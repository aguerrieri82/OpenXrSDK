#if ANDROID23_0_OR_GREATER

using Android.Media.Midi;
using Android.Net.IpSec.Ike.Exceptions;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;

namespace XrEngine.Devices.Android
{
    [SupportedOSPlatform("android23.0")]
    public class AndroidMidiInPort : IMidiInPort
    {
        MidiOutputPort _port;

        class Receiver : MidiReceiver
        {
            private readonly Action<byte[], int, int, long> _onReceive;
            
            public Receiver(Action<byte[], int, int, long> onReceive)
            {
                _onReceive = onReceive;
            }

            public override void OnSend(byte[]? data, int offset, int count, long timestamp)
            {
                _onReceive(data!, offset, count, timestamp);
            }

        }

        public AndroidMidiInPort(MidiOutputPort port)
        {
            _port = port;
            _port.Connect(new Receiver(OnReceive));

        }


        protected void OnReceive(byte[] data, int offset, int count, long timestamp)
        {
            DataReceived?.Invoke(this, new MidiData
            {
                Data = data,
                Offset = offset,
                Count = count,
                Timestamp = timestamp
            });
        }


        public void Close()
        {
            _port.Close();
        }


        public event EventHandler<MidiData>? DataReceived;
    }
}

#endif