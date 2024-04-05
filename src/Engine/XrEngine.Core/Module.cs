using XrEngine;
using XrEngine.Services;

[assembly: Module(typeof(XrEngine.Module))]

namespace XrEngine
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement(AssetLoader.Instance);
            Context.Implement(ModuleManager.Instance);
            Context.Implement(ObjectManager.Instance);
            Context.Implement(TypeStateManager.Instance);
        }
    }
}

