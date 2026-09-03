namespace XrEngine
{

    [Flags]
    public enum ScreenRefractionFlags
    {
        None = 0x0,
        Stereo = 0x1,
        Transform = 0x2,
        External = 0x4
    }

    public interface IScreenRefractionSource
    {
        ScreenRefractionFlags Flags { get; }

        Texture2D?[] GetRefractionTextures(PerspectiveCamera camera);

        int Priority { get; }
    }
}
