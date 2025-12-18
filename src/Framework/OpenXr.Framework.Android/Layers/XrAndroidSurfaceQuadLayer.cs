using Android.Runtime;
using Android.Views;
using Common.Interop;
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
        protected NativeStruct<CompositionLayerImageLayoutFB> _layerFlags;

        protected XrAndroidSurfaceQuadLayer(GetQuadDelegate getQuad)
            : base(getQuad)
        {

        }

        public XrAndroidSurfaceQuadLayer(Extent2Di size, GetQuadDelegate getQuad)
            : this(getQuad)
        {
            _size = size;
        }

        public unsafe override void Initialize(XrApp app, IList<string> extensions)
        {
            extensions.Add(KhrAndroidSurfaceSwapchain.ExtensionName);
            extensions.Add("XR_FB_android_surface_swapchain_create");

            var driver = app.Plugin<IXrGraphicDriver>().SwapChainImageType.StructureType;

            if (driver == StructureType.SwapchainImageVulkanKhr)
            {
                extensions.Add("XR_FB_composition_layer_image_layout");

                _layerFlags.Value = new CompositionLayerImageLayoutFB
                {
                    Type = StructureType.CompositionLayerImageLayoutFB,
                    Flags = CompositionLayerImageLayoutFlagsFB.VerticalFlipBitFB,
                    Next = null
                };

                StructChain.AddNextStruct(ref _header.ValueRef, _layerFlags.Pointer);
            }

            base.Initialize(app, extensions);
        }

        public override void OnBeginFrame(Silk.NET.OpenXR.Space space, long displayTime)
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
                CreateFlags = AndroidSurfaceSwapchainFlagsFB.None,
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

            _header.ValueRef.SubImage.Swapchain = _swapchain;
            _header.ValueRef.SubImage.ImageArrayIndex = 0;
            _header.ValueRef.SubImage.ImageRect.Extent = _size;

            _header.ValueRef.EyeVisibility = EyeVisibility.Both;
            _header.ValueRef.LayerFlags = CompositionLayerFlags.BlendTextureSourceAlphaBit;
        }

        public override void Dispose()
        {
            _layerFlags.Dispose();
            base.Dispose();
        }

        public Surface? Surface => _surface;

    }
}
