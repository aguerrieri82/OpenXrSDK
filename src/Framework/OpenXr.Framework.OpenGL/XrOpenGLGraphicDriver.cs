using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.OpenGL
{
    public class XrOpenGLGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IDisposable
    {
        protected GraphicsBinding _binding;
        protected IOpenGLDevice _device;
        protected XrApp? _app;
        protected KhrOpenglEnable? _openGl;
        protected XrDynamicType _swapChainType;

        protected GLEnum[] _validFormats = [
           GLEnum.Rgb10A2,
           GLEnum.Rgba16f,
           GLEnum.Rgba8,
           GLEnum.Rgba8SNorm];

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

            _device.Initialize(req.MinApiVersionSupported, req.MaxApiVersionSupported);

            return _device.View.CreateOpenGLBinding();
        }


        public long SelectSwapChainFormat(IList<long> availFormats)
        {
            return (long)_validFormats.First(a => availFormats.Contains((long)a));
        }

        public void Dispose()
        {
            _device.Dispose();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;

        public IOpenGLDevice Device => _device; 

    }
}
