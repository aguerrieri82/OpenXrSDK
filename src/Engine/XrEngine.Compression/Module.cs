using XrEngine;
using XrEngine.Transcoder;

[assembly: Module(typeof(XrEngine.Compression.Module))]

namespace XrEngine.Compression
{
    public class Module : IModule
    {
        public void Load()
        {
            var assetLoader = AssetLoader.Instance;

            assetLoader.Register(PngReader.Instance);
        }

        public void Shutdown()
        {

        }
    }
}

