using System.Numerics;
using System.Runtime.CompilerServices;
using XrEngine;
using XrEngine.OpenXr;
using XrSamples;
using XrSamples.Dnd;
using XrSamples.Graffiti;

namespace XrEditor
{
    public static class EditorDebug
    {
        public static readonly GraphicDriver Driver = GraphicDriver.OpenGL;

        public static readonly bool AutoStartApp = true;

        public static readonly bool EnableVSync = true;

        public static readonly bool EnablePreview = false;

        public static readonly bool UseEs = false;

        public static readonly bool DebugSync = true;

        public static readonly bool DebugEnabled = true;

        public static readonly bool DisableDualRender = true;

        public static readonly int VSyncScale = 3; 


        public static readonly string StoragePath = "D:\\Projects\\XrEditor\\Storage";


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
                  opt.Compression.BlockSize = 3;
                  opt.Compression.Quality = 80;
                  
                  opt.ShadowMap.Mode = ShadowMapMode.Hard;
                  opt.ShadowMap.BiasMode = ShadowMapBiasMode.None;
                  opt.ShadowMap.UseShadowSampler = false;

                  opt.ContactShadow.Use = false;
                  opt.ContactShadow.IsMultiView = false;

                  opt.UseResolve = false;
                  opt.ToneMap = ToneMapMode.Neutral;

              })
            // .UseSpaceWarp()
              .SetRenderQuality(1f, 2, false)
              .CreateLightField()  
              .Build();
    }
}
