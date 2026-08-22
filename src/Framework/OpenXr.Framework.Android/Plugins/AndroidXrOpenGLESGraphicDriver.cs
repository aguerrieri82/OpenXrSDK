using Silk.NET.Core.Contexts;
using Silk.NET.OpenGLES;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;

namespace OpenXr.Framework.Android
{
    public class AndroidXrOpenGLESGraphicDriver : XrBasePlugin, IXrGraphicDriver, IApiProvider
    {

#if DEBUG
        public const bool DEBUG_MODE = true;
#else
        public const bool DEBUG_MODE = false;
#endif

        protected OpenGLESContext _context;
        protected XrDynamicType _swapChainType;
        protected KhrOpenglEsEnable? _openGlEs;

        protected GLEnum[] _validFormats = [
           GLEnum.Srgb8Alpha8,
           GLEnum.Rgba8,
        ];

        public AndroidXrOpenGLESGraphicDriver()
            : this(OpenGLESContext.Create(DEBUG_MODE))
        {
        }

        public AndroidXrOpenGLESGraphicDriver(OpenGLESContext context)
        {
            _context = context;
            _swapChainType = new XrDynamicType
            {
                StructureType = StructureType.SwapchainImageOpenglESKhr,
                Type = typeof(SwapchainImageOpenGLESKHR)
            };
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            _app = app;
            extensions.Add(KhrOpenglEsEnable.ExtensionName);
        }

        public override void OnInstanceCreated()
        {
            if (!_app!.Xr.TryGetInstanceExtension<KhrOpenglEsEnable>(null, _app.Instance, out _openGlEs))
            {
                throw new NotSupportedException(KhrOpenglEsEnable.ExtensionName + " not supported");
            }
        }

        public override void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {
            System.Diagnostics.Debug.Assert(viewInfo.SwapChainFormats != null);

            var cast = viewInfo.SwapChainFormats!.Select(a => ((GLEnum)a).ToString()).ToArray();

            result.ColorFormat = (int)_validFormats.First(a => viewInfo.SwapChainFormats.Contains((int)a));

            if (result.DepthFormat == 0)
                result.DepthFormat = (int)InternalFormat.Depth24Stencil8;
        }

        public GraphicsBinding CreateBinding()
        {
            var req = new GraphicsRequirementsOpenGLESKHR
            {
                Type = StructureType.GraphicsRequirementsOpenglESKhr
            };

            _app!.CheckResult(_openGlEs!.GetOpenGlesgraphicsRequirements(_app!.Instance, _app.SystemId, ref req), "GetOpenGlesgraphicsRequirements");

            var result = new GraphicsBinding
            {
                Type = StructureType.GraphicsBindingOpenglESAndroidKhr,
                OpenGLESAndroidKhr = new()
                {
                    Type = StructureType.GraphicsBindingOpenglESAndroidKhr,
                    Display = (nint)_context.Display!.NativeHandle,
                    Config = (nint)_context.Config!.NativeHandle,
                    Context = (nint)_context.Context!.NativeHandle,
                    Next = null
                }
            };

            _context.Take();
            _context.SetSwapInterval(0);

            return result;
        }

        public T GetApi<T>() where T : class
        {
            if (typeof(T) == typeof(GL))
                return (T)(object)GL.GetApi(new MultiNativeContext(GL.CreateDefaultContext(["libGLESv2.so"]), new EglContext()));
            throw new NotSupportedException();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;

        public OpenGLESContext Context => _context;

    }
}
