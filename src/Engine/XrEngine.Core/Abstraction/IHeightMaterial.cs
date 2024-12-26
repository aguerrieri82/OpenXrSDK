namespace XrEngine
{
    public enum HeightNormalMode
    {
        Fast,
        Sobel
    }

    public interface IHeightMaterial : IMaterial, ITessellation
    {
        float HeightScale { get; set; }

        float HeightNormalStrength { get; set; }

        HeightNormalMode HeightNormalMode { get; set; }

        Texture2D? HeightMap { get; set; }
    }
}
