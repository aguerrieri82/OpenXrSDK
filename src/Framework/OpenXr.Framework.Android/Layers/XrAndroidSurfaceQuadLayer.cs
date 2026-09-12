using Android.Runtime;
using Android.Views;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using Space = Silk.NET.OpenXR.Space;

namespace OpenXr.Framework.Android
{
    public class XrAndroidSurfaceLayerSource : IGeometryLayerSource
    {
        protected Surface? _surface;
        protected KhrAndroidSurfaceSwapchain? _androidSurface;
        protected Extent2Di _size;
        protected SemaphoreSlim _surfaceLock = new(1, 1);
        protected Swapchain _swapchain;
        protected XrApp? _xrApp;

        public XrAndroidSurfaceLayerSource(Extent2Di size)
        {
            _size = size;
        }

        public void Initialize(XrApp app, IList<string> extensions)
        {
            _xrApp = app;

            extensions.Add(KhrAndroidSurfaceSwapchain.ExtensionName);
            extensions.Add("XR_FB_android_surface_swapchain_create");
        }

        public void OnBeginFrame(Space space, long displayTime)
        {
            _surfaceLock.Wait();
        }

        public void OnEndFrame()
        {
            _surfaceLock.Release();
        }

        public SwapchainSubImage Create()
        {
            _xrApp!.Xr.TryGetInstanceExtension<KhrAndroidSurfaceSwapchain>(null, _xrApp.Instance, out _androidSurface);

            var info = new SwapchainCreateInfo()
            {
                Type = StructureType.SwapchainCreateInfo,
                Width = (uint)_size.Width,
                Height = (uint)_size.Height,
            };

            nint surfaceHandle = 0;

            _xrApp.CheckResult(
                _androidSurface.CreateSwapchainAndroidSurface(
                    _xrApp.Session,
                    in info,
                    ref _swapchain,
                    ref surfaceHandle),
                "CreateSwapchainAndroidSurface");

            _surface = Java.Lang.Object.GetObject<Surface>(surfaceHandle, JniHandleOwnership.TransferGlobalRef)!;

            return new SwapchainSubImage
            {
                Swapchain = _swapchain,
                ImageArrayIndex = 0,
                ImageRect =
                {
                    Extent = _size
                }
            };
        }

        public virtual bool Update(long predTime)
        {
            return true;
        }

        public void Destroy()
        {
        }

        public Surface? Surface => _surface;
    }
}