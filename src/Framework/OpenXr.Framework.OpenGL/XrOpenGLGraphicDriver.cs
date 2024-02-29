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
        protected KhrOpenglEnable? _openGl;
        protected IOpenGLDevice _device;
        protected XrDynamicType _swapChainType;

        protected GLEnum[] _validFormats = [
           GLEnum.Srgb8Alpha8,
           GLEnum.Rgba8];


        public XrOpenGLGraphicDriver(IView view)
            : this(new ViewOpenGLDevice(view))
        {
        }

        public XrOpenGLGraphicDriver(IOpenGLDevice device)
        {
            _device = device;
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

            var binding = new GraphicsBinding
            {
                OpenGLWin32Khr = new GraphicsBindingOpenGLWin32KHR
                {
                    Type = StructureType.GraphicsBindingOpenglWin32Khr,
                    HDC = _device.HDc,
                    HGlrc = _device.GlCtx
                }
            };

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
                return (T)(object)_device.Gl;
            throw new NotSupportedException();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;
    }
}
