namespace XrEngine
{
    public enum IBLProcessMode
    {
        None = 0,
        Lambertian = 0x1,
        GGX = 0x2,
        Charlie = 0x4,
        Sheen = 0x8,
        All = Lambertian | GGX | Charlie | Sheen
    }

    public class PanoramaProcessorOptions
    {
        public static PanoramaProcessorOptions Default()
        {
            return new()
            {
                Resolution = 512,
                SampleCount = 1024,
                LodBias = 0f,
                MipLevelCount = 10,
                Mode = IBLProcessMode.Lambertian | IBLProcessMode.GGX | IBLProcessMode.Charlie,
                ShaderResolver = str => Embedded.GetString(str),
            };
        }

        public Func<string, string>? ShaderResolver { get; set; }

        public uint Resolution { get; set; }

        public uint SampleCount { get; set; }

        public float LodBias { get; set; }

        public uint MipLevelCount { get; set; }

        public IBLProcessMode Mode { get; set; }
    }

    public interface IIBLPanoramaProcessor
    {
        PbrMaterial.IBLTextures ProcessPanoramaIBL(TextureData data, PanoramaProcessorOptions options);
    }
}
