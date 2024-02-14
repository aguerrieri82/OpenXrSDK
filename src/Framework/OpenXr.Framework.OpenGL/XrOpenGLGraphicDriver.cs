using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using Silk.NET.Windowing;
using System.Diagnostics;

namespace OpenXr.Framework.OpenGL
{
    public class XrOpenGLGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IApiProvider
    {
        protected GraphicsBinding _binding;
        protected IView _view;
        protected XrApp? _app;
        protected KhrOpenglEnable? _openGl;
        protected XrDynamicType _swapChainType;

        protected GLEnum[] _validFormats = [
           GLEnum.Rgb10A2,
           GLEnum.Rgba16f,
           GLEnum.Rgba8,
           GLEnum.Rgba8SNorm];

        public XrOpenGLGraphicDriver(IView view)
        {
            _view = view;
            _swapChainType = new XrDynamicType
            {
                StructureType = StructureType.SwapchainImageOpenglKhr,
                Type = typeof(SwapchainImageOpenGLKHR)
            };
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            _app = app;
            extensions.Add(KhrOpenglEnable.ExtensionName);
        }

        public override void OnInstanceCreated()
        {
            if (!_app!.Xr.TryGetInstanceExtension<KhrOpenglEnable>(null, _app.Instance, out _openGl))
            {
                throw new NotSupportedException(KhrOpenglEnable.ExtensionName + " not supported");
            }
        }

        public GraphicsBinding CreateBinding()
        {
            var req = new GraphicsRequirementsOpenGLKHR
            {
                Type = StructureType.GraphicsRequirementsOpenglKhr
            };

            _app!.CheckResult(_openGl!.GetOpenGlgraphicsRequirements(_app!.Instance, _app.SystemId, ref req), "GetOpenGlgraphicsRequirements");

            var binding = _view.CreateOpenGLBinding();
            return binding;
        }

        public override void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {
            Debug.Assert(viewInfo.SwapChainFormats != null);

            result.SwapChainFormat = (long)_validFormats.First(a => viewInfo.SwapChainFormats.Contains((long)a));
        }


        public T GetApi<T>() where T : class
        {
            if (typeof(T) == typeof(GL))
                return (T)(object)_view.CreateOpenGL();
            throw new NotSupportedException();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;
    }
}
