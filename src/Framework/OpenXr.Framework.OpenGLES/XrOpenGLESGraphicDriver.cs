using Silk.NET.OpenGLES;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using Silk.NET.Windowing;

namespace OpenXr.Framework.OpenGLES
{
    public class XrOpenGLESGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IApiProvider
    {
        protected GraphicsBinding _binding;
        protected IView _view;
        protected XrApp? _app;
        protected KhrOpenglEsEnable? _openGlEs;
        protected XrDynamicType _swapChainType;

        protected GLEnum[] _validFormats = [
           GLEnum.Rgb10A2,
           GLEnum.Rgba16f,
           GLEnum.Rgba8,
           GLEnum.Rgba8SNorm];

        public XrOpenGLESGraphicDriver(IView view)
        {
            _view = view;
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

        public GraphicsBinding CreateBinding()
        {
            var req = new GraphicsRequirementsOpenGLESKHR
            {
                Type = StructureType.GraphicsRequirementsOpenglESKhr
            };

            _app!.CheckResult(_openGlEs!.GetOpenGlesgraphicsRequirements(_app!.Instance, _app.SystemId, ref req), "GetOpenGlesgraphicsRequirements");

            return _view.CreateOpenGLESBinding();
        }

        public long SelectSwapChainFormat(IList<long> availFormats)
        {
            return (long)_validFormats.First(a => availFormats.Contains((long)a));
        }

        public T GetApi<T>() where T : class
        {
            if (typeof(T) == typeof(GL))
                return (T)(object)_view.CreateOpenGLES();
            throw new NotSupportedException();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;

        public IView View => _view; 
    }
}
