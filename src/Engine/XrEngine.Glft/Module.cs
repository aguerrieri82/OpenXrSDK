using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.Services;

[assembly: Module(typeof(XrEngine.Gltf.Module))]

namespace XrEngine.Gltf
{
    public class Module : IModule
    {
        public void Load()
        {
            AssetLoader.Instance.Register(new GltfAssetLoader());
        }
    }
}

