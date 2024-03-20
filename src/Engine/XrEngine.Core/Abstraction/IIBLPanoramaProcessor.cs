using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public enum IBLProcessMode
    {
        None =0,
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
                Use8Bit = false,
                Mode = IBLProcessMode.Lambertian | IBLProcessMode.GGX | IBLProcessMode.Charlie,
                ShaderResolver = str => Embedded.GetString(str),
            };
        }

        public Func<string, string>? ShaderResolver;

        public bool Use8Bit;

        public uint Resolution;

        public uint SampleCount;

        public float LodBias;

        public uint MipLevelCount;

        public IBLProcessMode Mode;
    }

    public interface IIBLPanoramaProcessor
    {
        PbrMaterial.IBLTextures ProcessPanoramaIBL(TextureData data, PanoramaProcessorOptions options);
    }
}
