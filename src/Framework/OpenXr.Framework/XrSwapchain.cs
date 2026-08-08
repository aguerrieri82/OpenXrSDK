using Common.Interop;
using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;

namespace OpenXr.Framework
{
    public class XrSwapchain : IDisposable
    {
        protected readonly XrApp _xrApp;
        protected Swapchain _swapchain;
        protected long _lastPredictedTime;

        protected readonly int _usageCount = 0;
        protected int _curUsageCount = 0;
        protected uint _lastImageIndex;
        private NativeArray<SwapchainImageBaseHeader>? _images;

        public XrSwapchain(XrApp xrApp, int usageCount = 1)
        {
            _xrApp = xrApp;
            _usageCount = usageCount;
        }

        public void Create(Extent2Di size, int format, uint arraySize, SwapchainUsageFlags usage, SwapchainTarget target)
        {
            if (_swapchain.Handle != 0)
                _xrApp.DestroySwapchain(_swapchain);

            _swapchain = _xrApp.CreateSwapChain(size, format, arraySize, usage, target);

            _lastPredictedTime = 0;

            Format = format;
            ArraySize = arraySize;
            Size = size;
            Usage = usage;
        }

        [MemberNotNull(nameof(_images))]
        public NativeArray<SwapchainImageBaseHeader> EnumerateImages()
        {
            _images ??= _xrApp.EnumerateSwapchainImages(_swapchain);
            return _images;
        }

        public unsafe SwapchainImageBaseHeader* AcquireImageAndWait()
        {
            if (_images == null)
                EnumerateImages();

            var index = Acquire();

            var result = _images.ItemPointer((int)index);

            Wait();

            return result;
        }

        public uint Acquire()
        {
            var isNewFrame = _lastPredictedTime != _xrApp.FramePredictedDisplayTime;

            if (isNewFrame)
            {
                _curUsageCount = 0;
                _lastImageIndex = _xrApp.AcquireSwapchainImage(_swapchain);
                _lastPredictedTime = _xrApp.FramePredictedDisplayTime;
            }

            _curUsageCount++;

            return _lastImageIndex;
        }

        public void Wait()
        {
            if (_curUsageCount == 1)
                _xrApp.WaitSwapchainImage(_swapchain);
        }

        public void Release()
        {
            if (_curUsageCount == _usageCount)
                _xrApp.ReleaseSwapchainImage(_swapchain);
        }

        public void Dispose()
        {
            if (_swapchain.Handle != 0)
            {
                _xrApp.DestroySwapchain(_swapchain);
                _swapchain.Handle = 0;
            }

            _images?.Dispose();
            _images = null;

            GC.SuppressFinalize(this);
        }

        public static implicit operator Swapchain(XrSwapchain self)
        {
            return self._swapchain;
        }

        public int Format { get; protected set; }

        public uint ArraySize { get; protected set; }

        public Extent2Di Size { get; protected set; }

        public SwapchainUsageFlags Usage { get; protected set; }

        public NativeArray<SwapchainImageBaseHeader>? Images => _images;

        public bool IsCreated => _swapchain.Handle != 0;

        public uint LastImageIndex => _lastImageIndex;

        public Swapchain Value => _swapchain;
    }
}
