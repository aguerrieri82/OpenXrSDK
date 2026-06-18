using XrEngine;
using XrEngine.Media;
using XrEngine.OpenGL;

[assembly: Module(typeof(XrEngine.OpenXr.Module))]

namespace XrEngine.OpenXr
{
    public class Module : IModule
    {
        public void Load()
        {
            TypeStateManager.Instance.Register(new XrInputStateManager());

            Context.Implement<ICameraPoseProvider>(() => new OculusCameraPoseProvider());

            Context.Implement<IQuodDepthCull>(() => new QuodDepthCullProvider());

            Context.Implement<IDepthPointProvider>(() => new GlDepthPointProvider(OpenGLRender.Current!.GL));

            Embedded.Register(typeof(Module).Assembly);
        }

        public void Shutdown()
        {

        }
    }
}

