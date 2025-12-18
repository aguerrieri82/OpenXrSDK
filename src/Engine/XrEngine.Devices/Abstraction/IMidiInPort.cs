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

        ulong RefTimeMs { get; }

        event EventHandler<MidiData> DataReceived;
    }
}
