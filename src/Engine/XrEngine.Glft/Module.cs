using XrEngine;

[assembly: Module(typeof(XrEngine.Gltf.Module))]

namespace XrEngine.Gltf
{
    public class Module : IModule
    {
        public void Load()
        {
            AssetLoader.Instance.Register(GltfAssetLoader.Instance);
        }

        public void Shutdown()
        {

        }
    }
}

