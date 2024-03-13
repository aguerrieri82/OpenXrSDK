using Android.Runtime;
using Android.Views;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;

namespace OpenXr.Framework.Android
{
    public class XrAndroidSurfaceQuadLayer : XrBaseQuadLayer
    {
        protected Surface? _surface;
        protected KhrAndroidSurfaceSwapchain? _androidSurface;
        protected Extent2Di _size;
        protected SemaphoreSlim _surfaceLock = new(1, 1);

        protected XrAndroidSurfaceQuadLayer(GetQuadDelegate getQuad)
            : base(getQuad)
        {
        }

        public XrAndroidSurfaceQuadLayer(Extent2Di size, GetQuadDelegate getQuad)
            : base(getQuad)
        {
            _size = size;
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            extensions.Add(KhrAndroidSurfaceSwapchain.ExtensionName);
            extensions.Add("XR_FB_android_surface_swapchain_create");
            base.Initialize(app, extensions);
        }

        public override void OnBeginFrame()
        {
            _surfaceLock.Wait();
        }

        public override void OnEndFrame()
        {
            _surfaceLock.Release();
        }

        public unsafe override void Create()
        {
            _xrApp!.Xr.TryGetInstanceExtension<KhrAndroidSurfaceSwapchain>(null, _xrApp!.Instance, out _androidSurface);

            var info = new SwapchainCreateInfo()
            {
                Type = StructureType.SwapchainCreateInfo,
                Width = (uint)_size.Width,
                Height = (uint)_size.Height,
            };

            var fbInfo = new AndroidSurfaceSwapchainCreateInfoFB
            {
                Type = StructureType.AndroidSurfaceSwapchainCreateInfoFB,
            };

            //info.Next = &fbInfo;

            nint surfaceHandle = 0;

            _xrApp.CheckResult(
                _androidSurface.CreateSwapchainAndroidSurface(
                    _xrApp.Session,
                    in info,
                    ref _swapchain,
                    ref surfaceHandle),
                "CreateSwapchainAndroidSurface");

            _surface = Surface.GetObject<Surface>(surfaceHandle, JniHandleOwnership.TransferGlobalRef)!;

            _header->SubImage.Swapchain = _swapchain;
            _header->SubImage.ImageArrayIndex = 0;
            _header->SubImage.ImageRect.Extent = _size;
            _header->EyeVisibility = EyeVisibility.Both;
            _header->LayerFlags = CompositionLayerFlags.None;
        }

        public Surface? Surface => _surface;

    }
}
