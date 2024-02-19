#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System.Diagnostics;

namespace Xr.Engine.Editor
{
    public class XrGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IApiProvider
    {

        protected KhrOpenglEnable? _openGl;
        protected XrDynamicType _swapChainType;
        protected RenderHost _renderHost;

        protected GLEnum[] _validFormats = [
           GLEnum.Rgb10A2,
           GLEnum.Rgba16f,
           GLEnum.Rgba8,
           GLEnum.Rgba8SNorm];

        public XrGraphicDriver(RenderHost host)
        {
            _renderHost = host;
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
                    HDC = _renderHost.Hdc,
                    HGlrc = _renderHost.GlCtx
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
                return (T)(object)_renderHost.Gl!;
            throw new NotSupportedException();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;
    }
}
