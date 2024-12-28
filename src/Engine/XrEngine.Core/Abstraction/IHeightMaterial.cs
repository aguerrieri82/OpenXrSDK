namespace XrEngine
{
    public interface IHeightMaterial : IMaterial
    {
        public HeightMapSettings? HeightMap { get; set; }
    }
}
