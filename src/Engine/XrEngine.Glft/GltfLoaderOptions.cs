namespace XrEngine.Gltf
{
    public class GltfLoaderOptions : IAssetLoaderOptions
    {
        public GltfLoaderOptions()
        {
            UseMips = true;
            ConvertColorTextureSRgb = true;
            DisableTangents = false;
            MaterialFactory = _ => new PbrMaterial();
        }

        public Func<int, PbrMaterial> MaterialFactory { get; set; }

        public bool ConvertColorTextureSRgb { get; set; }

        public TextureFormat TextureFormat { get; set; }

        public bool DisableTangents { get; set; }

        public Type? PbrType { get; set; }

        public bool GeometryGpuOnly { get; set; }

        public bool UseCache { get; set; }

        public bool UseInstances { get; set; }

        public bool UseMips { get; set; }

        public bool TransmissionBkOnly { get; set; }

        public static readonly GltfLoaderOptions Default = new();
    }
}
