using OpenXr.Framework;
using XrEngine;
using XrEngine.OpenGL;

[assembly: Module(typeof(XrEngine.OpenXr.Module))]

namespace XrEngine.OpenXr
{
    public class Module : IModule
    {
        public void Load()
        {

            TypeStateManager.Instance.Register(new XrInputStateManager());

            Context.Implement<IDepthPointProvider>(() => new GlDepthPointProvider(OpenGLRender.Current!.GL));
        }

        public void Shutdown()
        {

        }
    }
}

