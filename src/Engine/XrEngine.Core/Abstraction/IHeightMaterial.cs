namespace XrEngine
{
    public interface IHeightMaterial : ITessellationMaterial
    {
        public DisplacmentMapSettings? DisplacmentMap { get; set; }
    }
}
