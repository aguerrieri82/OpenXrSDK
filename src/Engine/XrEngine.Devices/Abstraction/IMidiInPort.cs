using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Devices
{
    public struct MidiData
    {
        public byte[] Data;
        public int Offset;
        public int Count;
        public long Timestamp;
    }


    public interface IMidiInPort
    {
        void Close();

        event EventHandler<MidiData> DataReceived;
    }
}
