using System.Numerics;
using System.Runtime.CompilerServices;
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

        public static readonly bool DisableDualRender = true;
        

        public static readonly string[] AssetsPath = [
            @"Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Common\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Earth\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Graffiti\Assets\",
            @"D:\Projects\"];


        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static Vector3 Avg(Vector3 a, Vector3 b, Vector3 c)
        {
            return (a + b + c) / 2f;
        }

        public static XrEngineApp CreateApp() => new XrEngineAppBuilder()
              .UseMultiView()
              //.UseStereo()
              .SetGlOptions(opt =>
              {
                  Vector3 a = new Vector3(0, 1, 1);
                  Vector3 b = new Vector3(1, 1, 1);
                  Vector3 c = new Vector3(0, 0, 1);
                  Vector3 d =(a + b + c) / 2f;
                  Console.WriteLine(d);

                  opt.UsePlanarReflection = true;
                  opt.UseDepthPass = false;
                  opt.UseHitTest = true;
                  opt.FrustumCulling = true;
                  opt.SampleCount = 4;
                  opt.FloatPrecision = XrEngine.OpenGL.ShaderPrecision.High;
                  opt.IntPrecision = XrEngine.OpenGL.ShaderPrecision.High;

                  opt.Compression.Use = false;
                  opt.Compression.Format = TextureCompressionFormat.Etc2;

                  opt.ShadowMap.Mode = ShadowMapMode.Hard;
                  opt.ShadowMap.BiasMode = ShadowMapBiasMode.None;
                  opt.ShadowMap.UseShadowSampler = false;

                  opt.ContactShadow.Use = false;
                  opt.ContactShadow.IsMultiView = false;

                  opt.UseSRGB = true;
                  opt.ToneMap = ToneMapMode.Neutral;

              })
              .UseSpaceWarp()
              .SetRenderQuality(1f, 2, true)
              .CreateToneControl()  
               //.CreateDepthSnapeshot()
              .Build();
    }
}
