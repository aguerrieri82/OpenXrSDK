using XrEngine;
using XrEngine.Services;

[assembly: Module(typeof(XrEngine.Gltf.Module))]

namespace XrEngine.Gltf
{
    public class Module : IModule
    {
        public void Load()
        {
            AssetLoader.Instance.Register(GltfAssetLoader.Instance);
        }
    }
}

