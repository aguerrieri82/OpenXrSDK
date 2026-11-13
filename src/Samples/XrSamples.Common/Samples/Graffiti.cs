using GraffitiVR.Game;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Graffiti")]
        public static XrEngineAppBuilder CreateGraffiti(this XrEngineAppBuilder builder, bool reconstructMode = false)
        {
            GraffitiVRApp.Build(builder, reconstructMode);

            return builder;
        }
    }
}
