using XrEngine;
using XrEngine.OpenGL;

[assembly: Module(typeof(XrEngine.Lighting.Module))]

namespace XrEngine.Lighting
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

