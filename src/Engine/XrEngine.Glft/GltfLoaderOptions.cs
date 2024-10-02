namespace XrEngine.Gltf
{
    public class GltfLoaderOptions : IAssetLoaderOptions
    {
        public GltfLoaderOptions()
        {
            ConvertColorTextureSRgb = true;
            DisableTangents = false;
            UsePbrV2 = true;
        }

        public bool ConvertColorTextureSRgb { get; set; }

        public TextureFormat TextureFormat { get; set; }

        public bool DisableTangents { get; set; }   

        public bool UsePbrV2 { get; set; }  


        public static readonly GltfLoaderOptions Default = new();
    }
}
