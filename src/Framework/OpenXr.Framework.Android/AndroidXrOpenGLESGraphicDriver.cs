using Android.Runtime;
using Java.Lang;
using OpenXr.Framework.Abstraction;
using Silk.NET.OpenGLES;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Android
{
    public class AndroidXrOpenGLESGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IApiProvider
    {
        protected OpenGLESContext _context;
        protected XrDynamicType _swapChainType;
        protected KhrOpenglEsEnable? _openGlEs;
        protected XrApp? _app;

        protected GLEnum[] _validFormats = [
                 GLEnum.Rgb10A2,
                 GLEnum.Rgba16f,
                 GLEnum.Rgba8,
                 GLEnum.Rgba8SNorm];


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

        public GraphicsBinding CreateBinding()
        {
            var req = new GraphicsRequirementsOpenGLESKHR
            {
                Type = StructureType.GraphicsRequirementsOpenglESKhr
            };

            _app!.CheckResult(_openGlEs!.GetOpenGlesgraphicsRequirements(_app!.Instance, _app.SystemId, ref req), "GetOpenGlesgraphicsRequirements");

            var result = new GraphicsBinding();
            result.Type = StructureType.GraphicsBindingOpenglESAndroidKhr;
            result.OpenGLESAndroidKhr.Display = ((IJavaObject)_context.Display!).Handle;
            result.OpenGLESAndroidKhr.Config = ((IJavaObject)_context.Config!).Handle;
            result.OpenGLESAndroidKhr.Context = ((IJavaObject)_context.Context!).Handle;
            return result;
        }

        public long SelectSwapChainFormat(IList<long> availFormats)
        {
            return (long)_validFormats.First(a => availFormats.Contains((long)a));
        }

        public T GetApi<T>() where T : class
        {
            if (typeof(T) == typeof(GL))
                return (T)(object)GL.GetApi(GL.CreateDefaultContext(["libGLESv2.so"]));
            throw new NotSupportedException();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;

    }
}
