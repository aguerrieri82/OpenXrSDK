using XrEngine;

[assembly: Module(typeof(XrEngine.Animation.Module))]

namespace XrEngine.Animation
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement<ValueHandlerRegistry>();
        }

        public void Shutdown()
        {

        }
    }
}

