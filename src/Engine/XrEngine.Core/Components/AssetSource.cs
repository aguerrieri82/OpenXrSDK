namespace XrEngine
{
    public class AssetSource : BaseComponent<EngineObject>
    {
        public AssetSource()
        {
        }

        public AssetSource(IAsset asset)
        {
            Asset = asset;
        }

        public IAsset? Asset { get; set; }
    }
}
