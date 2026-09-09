using XrEngine;
using XrEngine.OpenXr;

namespace $ext_safeprojectname$.Windows
{
    public class Builder : IAppBuilder
    {
        public XrEngineApp Build(XrEngineAppBuilder builder)
        {
            return builder
                .UseOpenGL()
                .UseMultiView()
                .SetRenderQuality(1, 2)
                .CreateGame()
                .Build();
        }
    }
}
