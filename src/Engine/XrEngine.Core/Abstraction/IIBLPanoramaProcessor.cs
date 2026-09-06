namespace XrEngine
{
    [Flags]
    public enum IblProcessMode
    {
        None = 0,
        Lambertian = 0x1,
        GGX = 0x2,
        Charlie = 0x4,
        All = Lambertian | GGX | Charlie
    }

    public class PanoramaProcessorOptions
    {
        public static PanoramaProcessorOptions Default()
        {
            return new()
            {
                Resolution = 512,
                SampleCount = 1024,
                IrradianceSampleMul = 64,
                LodBias = 0f,
                MipLevelCount = 10,
                Mode = IblProcessMode.All,
                ShaderResolver = str => Embedded.GetString(str),
            };
        }

        public Func<string, string>? ShaderResolver { get; set; }

        public uint Resolution { get; set; }

        public uint SampleCount { get; set; }

        public uint IrradianceSampleMul { get; set; }

        public float LodBias { get; set; }

        public uint MipLevelCount { get; set; }

        public IblProcessMode Mode { get; set; }

    }

    public interface IIBLPanoramaProcessor
    {
        IBLTextures ProcessPanoramaIBL(TextureData data, PanoramaProcessorOptions options);
    }
}
