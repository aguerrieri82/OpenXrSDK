namespace XrEngine
{

    public enum TransmissionMode
    {
        None = 0,
        FrameBufferFetch = 1,
        DualAlpha = 2,
        Texture = 3,
        TextureBackground = 4
    }

    public interface ITransmissionMaterial : IMaterial
    {
        bool HasTransmission { get; }

        TransmissionMode TransmissionMode { get; }
    }
}
