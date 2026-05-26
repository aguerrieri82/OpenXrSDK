using System.Numerics;

namespace XrEngine
{
    public enum ShadowMapMode
    {
        None,
        Hard,
        PCF,
        VSM
    }

    public enum ShadowMapBiasMode
    {
        None = 0,
        Auto = 1,
        Value = 2
    }


    public class ShadowMapOptions
    {
        public ShadowMapMode Mode { get; set; }

        public ShadowMapBiasMode BiasMode { get; set; }

        [Range(0, 1, 0.01f)]
        public float Bias { get; set; }

        public uint Size { get; set; }

        public bool IsCasterMode { get; set; }

        public Vector3 Expand { get; set; }


        [Range(0, 1, 0.01f)]
        public float LightBleed { get; set; }

        public int BlurRadius { get; set; }

        public bool UseFrustumIntersect { get; set; }
    }

    public interface IShadowMapProvider
    {

        ShadowMapOptions Options { get; }

        Texture2D? ShadowMap { get; }

        Camera? LightCamera { get; }

        DirectionalLight? Light { get; }
    }
}
