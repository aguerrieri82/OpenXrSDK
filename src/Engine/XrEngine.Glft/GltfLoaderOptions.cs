namespace XrEngine.Gltf
{
    public class GltfLoaderOptions : IAssetLoaderOptions
    {
        public GltfLoaderOptions()
        {
            ConvertColorTextureSRgb = true;
            DisableTangents = false;
        }

        public bool ConvertColorTextureSRgb { get; set; }

        public TextureFormat TextureFormat { get; set; }

        public bool DisableTangents { get; set; }

        public Type? PbrType { get; set; }

        public bool GeometryGpuOnly { get; set; }   


        public static readonly GltfLoaderOptions Default = new();
    }
}
