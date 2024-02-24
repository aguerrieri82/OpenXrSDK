#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System.Diagnostics;

namespace Xr.Editor
{
    public class XrGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IApiProvider
    {

        protected KhrOpenglEnable? _openGl;
        protected XrDynamicType _swapChainType;
        protected IRenderSurface _renderSurface;

        protected GLEnum[] _validFormats = [
           GLEnum.Srgb8Alpha8,
           GLEnum.Rgba8];

        public XrGraphicDriver(IRenderSurface host)
        {
            _renderSurface = host;
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
                    HDC = _renderSurface.Hdc,
                    HGlrc = _renderSurface.GlCtx
                }
            };

            return binding;
        }

        public override void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {
            Debug.Assert(viewInfo.SwapChainFormats != null);

            var glFormat = viewInfo.SwapChainFormats.Select(a => (GLEnum)(int)a).ToArray();


            result.SwapChainFormat = (long)_validFormats.First(a => viewInfo.SwapChainFormats.Contains((long)a));
        }


        public T GetApi<T>() where T : class
        {
            if (typeof(T) == typeof(GL))
                return (T)(object)_renderSurface.Gl!;
            throw new NotSupportedException();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;
    }
}
