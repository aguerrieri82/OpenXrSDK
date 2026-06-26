using XrEngine;
using XrEngine.OpenXr;
using XrSamples;
using XrSamples.Dnd;

namespace XrEditor
{
    public static class EditorDebug
    {
        public static readonly GraphicDriver Driver = GraphicDriver.OpenGL;

        public static readonly bool AutoStartApp = true;

        public static readonly bool EnableVSync = true;

        public static readonly bool EnablePreview = true;

        public static readonly bool UseEs = false;

        public static readonly bool DebugSync = true;

        public static readonly bool DisableDualRender = false;
        

        public static readonly string[] AssetsPath = [
            @"Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Common\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Earth\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Graffiti\Assets\",
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
                  opt.SampleCount = 1;
                  opt.FloatPrecision = XrEngine.OpenGL.ShaderPrecision.High;
                  opt.IntPrecision = XrEngine.OpenGL.ShaderPrecision.High;

                  opt.Compression.Use = false;
                  opt.Compression.Format = TextureCompressionFormat.Etc2;

                  opt.ShadowMap.Mode = ShadowMapMode.Hard;
                  opt.ShadowMap.BiasMode = ShadowMapBiasMode.None;
                  opt.ShadowMap.UseShadowSampler = false;

                  opt.ContactShadow.Use = false;
                  opt.ContactShadow.IsMultiView = false;

              })
              .UseSpaceWarp()
              .SetRenderQuality(1f, 1)
              .CreateRoomManager()  
               //.CreateDepthSnapeshot()
              .Build();
    }
}
