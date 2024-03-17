using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static class XrEngineAppBuilderExtension
    {
        public static XrEngineAppBuilder RemovePlaneGrid(this XrEngineAppBuilder builder) => builder.ConfigureApp(e =>
        {
            e.App.ActiveScene!.Descendants<PlaneGrid>().First().IsVisible = false;
        });

    }
}
