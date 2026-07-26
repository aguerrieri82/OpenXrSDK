using XrEngine;
using XrEngine.OpenGL;
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
                  opt.FloatPrecision = ShaderPrecision.High;
                  opt.IntPrecision = ShaderPrecision.High;

                  opt.UseAsyncShaderCompile = true;
                  opt.UseShaderCache = true;
                  opt.UseShaderPreprocessor = true;

                  opt.Compression.Use = false;
                  opt.Compression.Format = TextureCompressionFormat.Etc2;

                  opt.ShadowMap.Mode = ShadowMapMode.PCF;
                  opt.ShadowMap.BiasMode = ShadowMapBiasMode.None;
                  opt.ShadowMap.UseShadowSampler = false;

                  opt.ContactShadow.Use = false;
                  opt.ContactShadow.IsMultiView = false;

                  opt.UseResolve = false;
                  opt.ToneMap = ToneMapMode.Neutral;

                  GlDebug.TrackBuffers = false;

              })
              // .UseSpaceWarp()
              .SetRenderQuality(1f, 2, useIntermediate: false)
              .CreateRoomManager()
              .Build();
    }
}
