﻿using Android.Runtime;
using Silk.NET.OpenGLES;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;

namespace OpenXr.Framework.Android
{
    public class AndroidXrOpenGLESGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IApiProvider
    {
        protected OpenGLESContext _context;
        protected XrDynamicType _swapChainType;
        protected KhrOpenglEsEnable? _openGlEs;

        protected GLEnum[] _validFormats = [
           GLEnum.Rgba8,
           GLEnum.Srgb8Alpha8,
           ];

        public AndroidXrOpenGLESGraphicDriver()
            : this(OpenGLESContext.Create())
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

            var cast = viewInfo.SwapChainFormats!.Select(a => ((GLEnum)(int)a).ToString()).ToArray();

            result.SwapChainFormat = (long)_validFormats.First(a => viewInfo.SwapChainFormats.Contains((long)a));
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

        public T GetApi<T>() where T : class
        {
            if (typeof(T) == typeof(GL))
                return (T)(object)GL.GetApi(GL.CreateDefaultContext(["libGLESv2.so"]));
            throw new NotSupportedException();
        }

        public XrDynamicType SwapChainImageType => _swapChainType;

        public OpenGLESContext Context => _context;

    }
}
