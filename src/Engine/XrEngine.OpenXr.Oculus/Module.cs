using XrEngine;
using XrEngine.OpenXr.Oculus;


[assembly: Module(typeof(XrEngine.OpenXr.Oculus.Module))]

namespace XrEngine.OpenXr.Oculus
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement<OculusPlatform>();
            Context.Implement<OculusAvatar>();

            // Embedded.Register(typeof(Module).Assembly);
        }

        public void Shutdown()
        {

        }
    }
}

