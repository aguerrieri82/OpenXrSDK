namespace XrEngine
{
    public interface IHeightMaterial : ITessellationMaterial
    {
        public HeightMapSettings? HeightMap { get; set; }
    }
}
