using XrMath;

namespace XrEngine
{
    public enum NormalMapFormat
    {
        Standard,
        UnityBc3
    }

    public interface IPbrMaterial : IMaterial, IColorSource, IShadowMaterial
    {
        Texture2D? ColorMap { get; set; }

        uint ColorMapUVSet { get; set; }

        Texture2D? MetallicRoughnessMap { get; set; }

        Texture2D? NormalMap { get; set; }

        NormalMapFormat NormalMapFormat { get; set; }

        Texture2D? OcclusionMap { get; set; }

        Color EmissiveColor { get; set; }

        float OcclusionStrength { get; set; }

        float NormalScale { get; set; }

        float Metalness { get; set; }

        float Roughness { get; set; }

        bool ToneMap { get; set; }

        float AlphaCutoff { get; set; }

    }
}