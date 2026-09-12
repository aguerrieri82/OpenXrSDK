
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Windows.Data.Xml.Dom;
using XrEngine;
using XrEngine.OpenXr;

#if DEVELOPMENT

using XrSamples;
using XrSamples.Dnd;
using XrSamples.Graffiti;
using XrEngine;
using XrEngine.OpenGL;

#endif

namespace XrEditor
{
    public  static class EditorDebug
    {
        public static readonly GraphicDriver Driver = GraphicDriver.OpenGL;

        public static readonly bool AutoStartApp = true;

        public static readonly bool EnableVSync = true;

        public static readonly int VSyncScale = 1;

        public static readonly bool EnablePreview = false;

        public static readonly bool IsMultiView = false;

#if GLES
        public static readonly bool UseEs = true;
#else
        public static readonly bool UseEs = false;
#endif

        public static readonly bool DisableDualRender = true;

        public static readonly bool UseDxHost = false;


#if DEVELOPMENT

        public static readonly string PersistentPath = "d:\\Projects\\XrEditor";

        public static readonly string StoragePath = Path.Combine(PersistentPath, "Storage");

        public static readonly string[] AssetsPath = [
            @"Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Common\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Earth\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Graffiti\Assets\",
            @"D:\Projects\"];


        public static XrEngineApp CreateApp() => new XrEngineAppBuilder()
              .When(IsMultiView, b => b.UseMultiView())
              .SetGlOptions(opt =>
              {
                  opt.UsePlanarReflection = true;
                  opt.UseDepthPass = false;
                  opt.UseHitTest = true;
                  opt.FrustumCulling = true;

                  opt.FloatPrecision = ShaderPrecision.High;
                  opt.IntPrecision = ShaderPrecision.High;

                  opt.UseAsyncShaderCompile = !IsMultiView || DisableDualRender;
                  opt.UseShaderCache = true;
                  opt.UseShaderPreprocessor = true;
                  opt.UseSharedSsbo = false;

                  opt.Compression.Use = false;
                  opt.Compression.Format = TextureCompressionFormat.Etc2;

                  opt.ShadowMap.Mode = ShadowMapMode.PCF;
                  opt.ShadowMap.BiasMode = ShadowMapBiasMode.None;
                  opt.ShadowMap.UseShadowSampler = false;

                  opt.ContactShadow.Use = false;
                  opt.ContactShadow.IsMultiView = IsMultiView;
                  
                  opt.ToneMap = ToneMapMode.Aces;
                  opt.UseProfiler = false;
                  opt.UseTransmission = true;

                  opt.UseFxAA = false;
                  opt.UseDefaultIntermediate = true;
                  opt.SampleCount = 1;

                  GlDebug.TrackBuffers = false;

                  TriangleMesh.EnableCompression = false;

                  if (Driver == GraphicDriver.Angle)
                  {
                  }

                  TypeUtils.Load<XrEngine.Media.Windows.Module>();
              })
              .UseOculus(opt =>
              {
              })
              .SetXrOptions(opt =>
              {
                  opt.UseSimmetricFov = false;
              })
              .SetAppOptions(opt =>
              {
                  opt.Driver = Driver;
              })
              //.UseSpaceWarp()
              .EnableDebugNotRelease(sync: true)
              .SetRenderQuality(1f, sampleCount: 2)
              .UseProjDepth(XrProjDepthMode.DepthCopyImage, 0.25f)
              .CreateDepthSnapeshotView()
              //.CreateDnd()
              .Build();


#else



        public static readonly string PersistentPath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XrEditor");

        public static readonly string StoragePath = 
            Path.Combine(PersistentPath, "Storage");

        public static readonly List<string> AssetsPath = [];


        public static XrEngineApp CreateApp()
        {

            var args = StartupArgs.Parse();

            var assemblyPaths = new List<string>();

            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                foreach (var path in assemblyPaths)
                {
    
                    var file = Path.Combine(path, $"{name.Name}.dll");

                    if (File.Exists(file))
                        return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file));
                }
                return null;
            };

            foreach (var assembly in args.Assemblies)
            {
                var fullPath = Path.GetFullPath(assembly);

                var path = Path.GetDirectoryName(fullPath)!;
                
                args.Assets.Add(Path.Combine(path, "Assets"));

                assemblyPaths.Add(path);
                AppDomain.CurrentDomain.Load(Assembly.LoadFile(fullPath).GetName());
            }

            var store = MergedAssetStore.FromLocalPaths(args.Assets.Select(Path.GetFullPath).ToArray());

            Context.Implement<IAssetStore>(store);

            var builder = new XrEngineAppBuilder();

            var app = AppEntryResolver.Build(args.Entry!, builder);

            return app;
        }
#endif
    }
}
