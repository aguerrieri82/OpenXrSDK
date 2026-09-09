using XrEngine;
using XrEngine.OpenXr;

namespace Game.Windows
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
