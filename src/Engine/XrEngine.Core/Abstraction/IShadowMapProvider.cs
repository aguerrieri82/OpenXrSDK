namespace XrEngine
{
    public enum ShadowMapMode
    {
        None,
        Hard,
        HardSmooth,
    }

    public class ShadowMapOptions
    {
        public ShadowMapMode Mode { get; set; }

        public uint Size { get; set; }
    }

    public interface IShadowMapProvider
    {

        ShadowMapOptions Options { get; }

        Texture2D? ShadowMap { get; }

        Camera? LightCamera { get; }

        DirectionalLight? Light { get; }
    }
}
