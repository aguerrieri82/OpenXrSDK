using XrEngine.OpenXr;
using XrSamples;

namespace XrEditor
{
    public static class EditorDebug
    {
        public static readonly GraphicDriver Driver = GraphicDriver.OpenGL;

        public static readonly bool AutoStartApp = true;

        public static readonly bool EnableVSync = true;

        public static readonly string[] AssetsPath = [
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Common\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Earth\Assets\"];  

        public static XrEngineApp CreateApp() => new XrEngineAppBuilder()
              //.UseMultiView()
              //.UseStereo()
              .SetGlOptions(opt =>
              {
                  opt.UsePlanarReflection = true;
                  opt.UseDepthPass = false;
                  opt.UseHitTest = true;
              })
              .SetRenderQuality(1, 2)
              .CreateDrums()
              .Build();
    }
}
