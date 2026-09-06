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
        protected int _lastImageIndex;
        protected NativeArray<SwapchainImageBaseHeader>? _images;
        protected SwapchainCreateInfo _info;

        public XrSwapchain(XrApp xrApp, int usageCount = 1)
        {
            _xrApp = xrApp;
            _usageCount = usageCount;
            _lastImageIndex = -1;
        }

        public void Create(Extent2Di size, int format, uint arraySize, SwapchainUsageFlags usage, SwapchainTarget target)
        {
            if (_swapchain.Handle != 0)
                _xrApp.DestroySwapchain(_swapchain);

            _swapchain = _xrApp.CreateSwapChain(size, format, arraySize, usage, target, (ref info) => _info = info);

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

            var result = _images.ItemPointer(index);

            Wait();

            return result;
        }

        public int Acquire()
        {
            var isNewFrame = _lastPredictedTime != _xrApp.FramePredictedDisplayTime;

            if (isNewFrame)
            {
                _curUsageCount = 0;
                _lastImageIndex = (int)_xrApp.AcquireSwapchainImage(_swapchain);
                _lastPredictedTime = _xrApp.FramePredictedDisplayTime;
                AfterAcquire?.Invoke(this);
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
            {
                BeforeRelease?.Invoke(this);
                _xrApp.ReleaseSwapchainImage(_swapchain);
            }
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

        public int LastImageIndex => _lastImageIndex;

        public unsafe SwapchainImageBaseHeader* LastImage
        {
            get
            {
                if (_images == null || _lastImageIndex == -1)
                    throw new InvalidOperationException();

                return _images.ItemPointer(_lastImageIndex);
            }
        }

        public Swapchain Value => _swapchain;

        public ref SwapchainCreateInfo CreateInfo => ref _info;

        public event Action<XrSwapchain>? BeforeRelease;

        public event Action<XrSwapchain>? AfterAcquire;
    }
}
