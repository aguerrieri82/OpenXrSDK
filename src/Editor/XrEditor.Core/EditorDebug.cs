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

        public static readonly bool EnableVSync = false;

        public static readonly int VSyncScale = 4;

        public static readonly bool EnablePreview = false;

        public static readonly bool UseEs = false;

        public static readonly bool DebugSync = true;

        public static readonly bool DebugEnabled = true;

        public static readonly bool DisableDualRender = true;

        public static readonly bool UseDxHost = true;

        public static readonly string PersistentPath = "d:\\Projects\\XrEditor";

        public static readonly string StoragePath = Path.Combine(PersistentPath, "Storage");

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
                  opt.SampleCount = 4;
                  opt.FloatPrecision = XrEngine.OpenGL.ShaderPrecision.High;
                  opt.IntPrecision = XrEngine.OpenGL.ShaderPrecision.High;

                  opt.Compression.Use = false;
                  opt.Compression.Format = TextureCompressionFormat.Astc;

                  opt.ShadowMap.Mode = ShadowMapMode.PCF;
                  opt.ShadowMap.BiasMode = ShadowMapBiasMode.None;
                  opt.ShadowMap.UseShadowSampler = false;

                  opt.ContactShadow.Use = false;
                  opt.ContactShadow.IsMultiView = false;

                  opt.UseResolve = false;
                  opt.UseSRGB = true;
                  opt.ToneMap = ToneMapMode.Neutral;

              })
              // .UseSpaceWarp()
              .SetRenderQuality(1f, 2, useIntermediate: false)
              .CreateRoomManager()
              .Build();
    }
}
