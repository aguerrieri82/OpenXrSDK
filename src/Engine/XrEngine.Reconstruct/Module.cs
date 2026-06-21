using XrEngine;


[assembly: Module(typeof(XrEngine.Reconstruct.Module))]

namespace XrEngine.Reconstruct
{
    public class Module : IModule
    {
        public void Load()
        {
            Embedded.Register(typeof(Module).Assembly);
        }

        public void Shutdown()
        {

        }
    }
}

