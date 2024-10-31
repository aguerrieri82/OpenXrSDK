using XrEngine;

[assembly: Module(typeof(XrEngine.OpenXr.Module))]

namespace XrEngine.OpenXr
{
    public class Module : IModule
    {
        public void Load()
        {
            TypeStateManager.Instance.Register(new XrInputStateManager());
        }

        public void Shutdown()
        {

        }
    }
}

