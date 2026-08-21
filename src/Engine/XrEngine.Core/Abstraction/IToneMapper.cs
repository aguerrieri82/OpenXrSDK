namespace XrEngine
{
    public interface IToneMapper
    {
        bool IsGlobal { get; }

        ToneMapMode ToneMap { get; set; }

        bool ResolveAlpha { get; set; }

        bool EncodeSrgb { get; set; }
    }
}
