using XrEngine;

[assembly: Module(typeof(XrSamples.Common.Module))]

namespace XrSamples.Common
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

