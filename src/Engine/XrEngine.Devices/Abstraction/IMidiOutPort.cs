namespace XrEngine.Devices
{
    public interface IMidiOutPort
    {
        void Send(byte[] data, int offset, int count);

        void Close();
    }
}
