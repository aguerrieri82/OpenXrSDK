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

        public static readonly int VSyncScale = 3;

        public static readonly bool EnablePreview = true;

#if GLES
        public static readonly bool UseEs = true;
#else
        public static readonly bool UseEs = false;
#endif

        public static readonly bool DisableDualRender = true;

        public static readonly bool UseDxHost = false;

        public static readonly string PersistentPath = "d:\\Projects\\XrEditor";

        public static readonly string StoragePath = Path.Combine(PersistentPath, "Storage");

        public static readonly string[] AssetsPath = [
            @"Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Common\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Earth\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Graffiti\Assets\",
            @"D:\Projects\"];

        public static XrEngineApp CreateApp() => new XrEngineAppBuilder()
             // .UseMultiView()
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
                  opt.UseProfiler = false;
                  opt.UseDefaultIntermediate = true;
                  opt.UseTransmission = true;

                  GlDebug.TrackBuffers = false;

                  if (Driver == GraphicDriver.Angle)
                  {
                  }

              })
              .UseOculus(opt =>
              {

              })
              .SetAppOptions(opt =>
              {
                  opt.Driver = Driver;
              })
              //.UseSpaceWarp()
              //.AddProfileOverlay()
              .EnableDebugNotRelease(sync: true)
              .SetRenderQuality(2f, 1, useIntermediate: false)
              .UseProjDepth(XrProjDepthMode.DepthCopyImage, 0.25f)
              .CreateGltfTest("Models/DragonAttenuation.glb")
              //   .CreateChess()
              .Build();
    }
}
