using XrEngine;


[assembly: Module(typeof(XrEngine.Tiff.Module))]

namespace XrEngine.Tiff
{
    public class Module : IModule
    {
        public void Load()
        {
            var assetLoader = AssetLoader.Instance;

            assetLoader.Register(TiffReader.Instance);
        }

        public void Shutdown()
        {

        }
    }
}

