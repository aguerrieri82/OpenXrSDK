namespace XrEngine.Devices
{
    public interface IMidiDevice
    {
        Task OpenAsync();

        void Close();

        IMidiOutPort OpenOutput(int index);

        IMidiInPort OpenInput(int index);

        int InputPortCount { get; }

        int OutputPortCount { get; }

        string Id { get; }
    }
}
