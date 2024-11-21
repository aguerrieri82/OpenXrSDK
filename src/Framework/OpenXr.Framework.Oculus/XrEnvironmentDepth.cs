using Common.Interop;
using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;
using XrMath;

namespace OpenXr.Framework.Oculus
{
    public class XrEnvironmentDepth : IDisposable
    {
        protected XrApp? _app;
        protected METAEnvironmentDepth? _envDepth;
        protected EnvironmentDepthProviderMETA _depthProvider;
        protected EnvironmentDepthSwapchainMETA _swapchain;
        protected NativeArray<SwapchainImageBaseHeader>? _images;
        private Size2 _size;
        private bool _isStarted;

        public XrEnvironmentDepth()
        {
        }

        public void Create(XrApp app)
        {
            _envDepth = new METAEnvironmentDepth(app.Xr, app.Instance);
            _app = app;
        }

        public void Start()
        {
            EnsureInit();

            if (_depthProvider.Handle == 0)
                _depthProvider = CreateProvider();

            if (_swapchain.Handle == 0)
            {
                _swapchain = CreateSwapChain();
                _images = EnumerateSwapchainImages(_swapchain);

                var state = new EnvironmentDepthSwapchainStateMETA
                {
                    Type = StructureType.EnvironmentDepthSwapchainStateMeta,
                };

                _app.CheckResult(_envDepth.GetEnvironmentDepthSwapchainStateMETA(_swapchain, out state), "GetEnvironmentDepthSwapchainStateMETA");
                _size = new Size2(state.Width, state.Height);
            }

            _app.CheckResult(_envDepth.StartEnvironmentDepthProviderMETA(_depthProvider), "StartEnvironmentDepthProviderMETA");

            _isStarted = true;
        }

        public void Stop()
        {
            EnsureInit();

            _app.CheckResult(_envDepth.StopEnvironmentDepthProviderMETA(_depthProvider), "StopEnvironmentDepthProviderMETA");

            _isStarted = false;
        }

        public EnvironmentDepthImageMETA Acquire(Space space, long displayTime)
        {
            EnsureInit();

            var acquireInfo = new EnvironmentDepthImageAcquireInfoMETA
            {
                Type = StructureType.EnvironmentDepthImageAcquireInfoMeta,
                DisplayTime = displayTime,
                Space = space
            };

            var result = new EnvironmentDepthImageMETA
            {
                Type = StructureType.EnvironmentDepthImageMeta,
            };

            result.Views[0].Type = StructureType.EnvironmentDepthImageViewMeta;
            result.Views[1].Type = StructureType.EnvironmentDepthImageViewMeta;

            _app.CheckResult(_envDepth.AcquireEnvironmentDepthImageMETA(_depthProvider, ref acquireInfo, out result), "AcquireEnvironmentDepthImageMETA");

            return result;
        }

        protected EnvironmentDepthProviderMETA CreateProvider()
        {
            EnsureInit();

            var info = new EnvironmentDepthProviderCreateInfoMETA
            {
                Type = StructureType.EnvironmentDepthProviderCreateInfoMeta,
                CreateFlags = EnvironmentDepthProviderCreateFlagsMETA.None,
            };

            _app.CheckResult(_envDepth.CreateEnvironmentDepthProviderMETA(_app.Session, ref info, out var depthProvider), "CreateEnvironmentDepthProviderMETA");

            return depthProvider;
        }


        protected unsafe NativeArray<SwapchainImageBaseHeader> EnumerateSwapchainImages(EnvironmentDepthSwapchainMETA swapchain)
        {
            EnsureInit();

            _app.CheckResult(_envDepth.EnumerateEnvironmentDepthSwapchainImagesMETA(swapchain, 0, out var count, null), "EnumerateEnvironmentDepthSwapchainImagesMETA");

            var imageType = _app.Plugin<IXrGraphicDriver>().SwapChainImageType;
            var images = new NativeArray<SwapchainImageBaseHeader>((int)count, imageType.Type!);

            for (var i = 0; i < images.Length; i++)
            {
                images.Item(i).Type = imageType.StructureType;
                images.Item(i).Next = null;
            }

            _app.CheckResult(_envDepth.EnumerateEnvironmentDepthSwapchainImagesMETA(swapchain, count, out count, images.Pointer), "EnumerateEnvironmentDepthSwapchainImagesMETA");

            return images;
        }

        protected EnvironmentDepthSwapchainMETA CreateSwapChain()
        {
            EnsureInit();

            var info = new EnvironmentDepthSwapchainCreateInfoMETA
            {
                Type = StructureType.EnvironmentDepthSwapchainCreateInfoMeta,
                CreateFlags = EnvironmentDepthSwapchainCreateFlagsMETA.None,
                Next = null
            };

            var swapchain = new EnvironmentDepthSwapchainMETA();

            _app.CheckResult(_envDepth.CreateEnvironmentDepthSwapchainMETA(_depthProvider, ref info, out swapchain), "CreateEnvironmentDepthSwapchainMETA");

            return swapchain;
        }

        [MemberNotNull(nameof(_app), nameof(_envDepth))]
        protected void EnsureInit()
        {
            if (_app == null || _envDepth == null)
                throw new InvalidOperationException("Not initialized");
        }

        public void Dispose()
        {
            if (_app == null || _envDepth == null)
                return;

            if (_swapchain.Handle != 0)
            {
                _app.CheckResult(_envDepth.DestroyEnvironmentDepthSwapchainMETA(_swapchain), "DestroyEnvironmentDepthSwapchainMETA");
                _swapchain.Handle = 0;
            }

            if (_depthProvider.Handle != 0)
            {
                _app.CheckResult(_envDepth.DestroyEnvironmentDepthProviderMETA(_depthProvider), "DestroyEnvironmentDepthProviderMETA");
                _depthProvider.Handle = 0;
            }
        }

        public void RemoveHand(bool value)
        {
            EnsureInit();

            var info = new EnvironmentDepthHandRemovalSetInfoMETA
            {
                Enabled = value ? 1u : 0,
                Type = StructureType.EnvironmentDepthHandRemovalSetInfoMeta
            };

            _app.CheckResult(_envDepth.SetEnvironmentDepthHandRemovalMETA(_depthProvider, ref info), "SetEnvironmentDepthHandRemovalMETA");
        }

        internal void RemoveHand(object removeHand)
        {
            throw new NotImplementedException();
        }

        public Size2 Size => _size;

        public NativeArray<SwapchainImageBaseHeader>? Images => _images;

        public bool IsStarted => _isStarted;
    }
}
