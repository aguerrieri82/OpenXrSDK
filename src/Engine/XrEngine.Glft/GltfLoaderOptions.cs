namespace XrEngine.Gltf
{
    public class GltfLoaderOptions : IAssetLoaderOptions
    {
        public GltfLoaderOptions()
        {
            ConvertColorTextureSRgb = true;
        }

        public bool ConvertColorTextureSRgb { get; set; }

        public TextureFormat TextureFormat { get; set; }

        public static readonly GltfLoaderOptions Default = new();
    }
}
