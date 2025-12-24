using XrEngine.OpenXr;
using XrSamples;

namespace XrEditor
{
    public static class EditorDebug
    {
        public static readonly GraphicDriver Driver = GraphicDriver.OpenGL;

        public static readonly bool AutoStartApp = true;

        public static readonly bool EnableVSync = true;

        public static readonly bool EnablePreview = false;

        public static readonly bool UseEs = false;

        public static readonly bool DebugSync = false;


        public static readonly string[] AssetsPath = [
            @"Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Common\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Earth\Assets\",
            @"D:\Projects\"];

        public static XrEngineApp CreateApp() => new XrEngineAppBuilder()
              //.UseMultiView()
              //.UseStereo()
              .SetGlOptions(opt =>
              {
                  opt.UsePlanarReflection = true;
                  opt.UseDepthPass = false;
                  opt.UseHitTest = true;
                  opt.FrustumCulling = true;
                  opt.UseLayerV2 = true;
                  opt.Compression.Use = true;
                  opt.Compression.Format = XrEngine.TextureCompressionFormat.Etc2;
              })
              .UseSpaceWarp()
              .SetRenderQuality(1f, 2)
              .CreateRoomManager()
              .Build();
    }
}
