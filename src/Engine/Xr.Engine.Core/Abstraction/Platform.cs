namespace Xr.Engine
{
    public class Platform
    {
        public IAssetManager? AssetManager { get; set; }

        public static Platform? Current { get; set; }
    }
}
