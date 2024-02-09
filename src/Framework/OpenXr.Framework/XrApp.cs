using Silk.NET.Core.Native;
using Silk.NET.Core;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Xml.Linq;


namespace OpenXr.Framework
{

    public enum XrAppStartMode
    {
        Render,
        Query
    }

    public unsafe class XrApp : IDisposable, IXrSession
    {
        const long DurationInfinite = 0x7fffffffffffffff;

        public bool CheckResult(Result result, string context)
        {
            if (result != Result.Success)
                throw new OpenXrException(result, context);

            _logger.LogDebug("{context} OK", context);
            
            return true;
        }

        protected Instance _instance;
        protected ulong _systemId;
        protected XR? _xr;
        protected List<string> _extensions;
        protected Session _session;
        protected IXrPlugin[] _plugins;
        protected XrViewInfo? _viewInfo;
        protected XrRenderOptions? _renderOptions;
        protected EnvironmentBlendMode _blendMode;
        protected Space _head;
        protected Space _local;
        protected Space _stage;
        protected bool _isStarted;
        protected SyncEvent _instanceReady;
        protected SessionState _lastSessionState;
        protected object _sessionLock;
        protected bool _sessionBegun;
        protected ILogger _logger;
        protected XrLayerManager _layers;
        protected View[]? _views;
        protected XrSwapchainInfo[]? _swapchains;
        protected bool _isDisposed;

        public XrApp(params IXrPlugin[] plugins)
            : this(NullLogger<XrApp>.Instance, plugins)
        {

        }

        public XrApp(ILogger logger, params IXrPlugin[] plugins)
        {
            _extensions = [];
            _logger = logger;
            _plugins = plugins;
            _lastSessionState = SessionState.Unknown;
            _instanceReady = new SyncEvent();
            _sessionLock = new object();
            _layers = new XrLayerManager(this);
        }

        protected void Initialize()
        {
            _xr = XR.GetApi();

            foreach (var plugin in _plugins)
                plugin.Initialize(this, _extensions);

            var supportedExtensions = GetSupportedExtensions();

            for (int i = 0; i < _extensions.Count; i++)
            {
                if (!supportedExtensions.Contains(_extensions[i]))
                {
                    _logger.LogWarning("{ext} not supported", _extensions[i]);
                    _extensions.RemoveAt(i);
                    i--;
                }
            }

            CreateInstance(AppDomain.CurrentDomain.FriendlyName, "OpenXr.Framework", _extensions);

            GetSystemId();

            CollectAndSelectView();

            PluginInvoke(p => p.OnInstanceCreated());

            _instanceReady.Signal();
        }

        public virtual void Start(XrAppStartMode mode = XrAppStartMode.Render)
        {
            if (_isStarted)
                return;

            if (_xr == null)
                Initialize();

            CreateSession();

            PluginInvoke(p => p.OnSessionCreated());

            WaitForSession(SessionState.Ready, SessionState.Focused);

            BeginSession(_viewInfo!.Type);

            if (mode == XrAppStartMode.Render)
            {
                _swapchains = new XrSwapchainInfo[_viewInfo.ViewCount];

                for (var i = 0; i < _swapchains.Length; i++)
                {
                    var info = new XrSwapchainInfo();
                    info.Swapchain = CreateSwapChain();
                    info.Images = EnumerateSwapchainImages(info.Swapchain);
                    info.Size = _renderOptions!.Size;
                    _swapchains[i] = info;
                }

                _views = new View[_viewInfo.ViewCount];
            }

            _isStarted = true;
        }

        public void RenderFrame()
        {
            var state = WaitFrame();
            
            BeginFrame();

            CompositionLayerBaseHeader*[]? layers = null;

            uint layerCount = 0;

            if (state.ShouldRender != 0)
            {
                var viewsState = LocateViews(_local, state.PredictedDisplayTime);

                var isPosValid = (viewsState.ViewStateFlags & ViewStateFlags.OrientationValidBit) != 0 &&
                                 (viewsState.ViewStateFlags & ViewStateFlags.PositionValidBit) != 0;

                if (isPosValid)
                    layers = _layers.Render(ref _views!, _swapchains!, out layerCount);

            }

            EndFrame(state.PredictedDisplayPeriod, ref layers, layerCount);
        }


        protected void EndFrame(long displayTime, ref CompositionLayerBaseHeader*[]? layers, uint count)
        {
            fixed (CompositionLayerBaseHeader** pLayers = layers)
            {
                var frameEndInfo = new FrameEndInfo()
                {
                    Type = StructureType.FrameEndInfo,
                    EnvironmentBlendMode = _blendMode,
                    DisplayTime = displayTime,
                    LayerCount = count,
                    Layers = pLayers
                };

                CheckResult(_xr!.EndFrame(_session, in frameEndInfo), "EndFrame");
            }
        }

        protected void BeginFrame()
        {
            var info = new FrameBeginInfo()
            {
                Type = StructureType.FrameBeginInfo
            };
            CheckResult(_xr!.BeginFrame(_session, in info), "BeginFrame");
        }

        protected FrameState WaitFrame()
        {
            var info = new FrameWaitInfo()
            {
                Type = StructureType.FrameWaitInfo
            };

            var result = new FrameState
            {
                Type = StructureType.FrameState
            };
            CheckResult(_xr!.WaitFrame(_session, in info, ref result), "WaitFrame");
            return result;
        }

        public virtual void Stop()
        {
            if (!_isStarted)
                return;

            _logger.LogDebug("Stopping");

            DisposeSpace(_local);
            DisposeSpace(_head);
            DisposeSpace(_stage);

            if (_session.Handle != 0)
            {
                CheckResult(_xr!.DestroySession(Session), "DestroySession");
                _session.Handle = 0;
            }

            if (_swapchains != null)
            {
                foreach (var item in _swapchains)
                {
                    CheckResult(_xr!.DestroySwapchain(item.Swapchain), "DestroySwapchain");
                    item.Images!.Dispose();
                }
               
                _swapchains = null;
            }

            _isStarted = false;
            _lastSessionState = SessionState.Unknown;
            _sessionBegun = false;

            PluginInvoke(p => p.OnSessionEnd());

            _logger.LogInformation("Stopped");

        }

        public bool HandleEvents()
        {
            return HandleEvents(CancellationToken.None);
        }

        public bool HandleEvents(CancellationToken cancellationToken)
        {
            if (!_instanceReady.Wait(cancellationToken))
                return true;

            if (_instance.Handle == 0)
                return false;

            var buffer = new EventDataBuffer();

            while (!cancellationToken.IsCancellationRequested)
            {
                buffer.Type = StructureType.EventDataBuffer;

                var result = _xr!.PollEvent(_instance, ref buffer);
                if (result != Result.Success)
                    break;

                _logger.LogDebug("New event {ev}", buffer.Type);

                try
                {
                    switch (buffer.Type)
                    {
                        case StructureType.EventDataSessionStateChanged:
                            var sessionChanged = buffer.Convert().To<EventDataSessionStateChanged>();
                            _session = sessionChanged.Session;
                            OnSessionChanged(sessionChanged.State, sessionChanged.Time);

                            break;
                    }

                    PluginInvoke(p => p.HandleEvent(ref buffer));
                }
                catch (Exception ex)
                {
                    _logger.LogError("Handling event {type}: {ex}", buffer.Type, ex);
                }

            }

            return true;
        }

        protected void WaitForSession(params SessionState[] states)
        {
            lock (_sessionLock)
            {
                while (!states.Contains(_lastSessionState))
                    Monitor.Wait(_sessionLock);
            }
        }

        protected void DisposeSpace(Space space)
        {
            if (space.Handle != 0)
            {
                CheckResult(_xr!.DestroySpace(space), "DestroySpace");
                space.Handle = 0;
            }
        }
        protected void PluginInvoke(Action<IXrPlugin> action)
        {
            PluginOfTypeInvoke(action);
        }

        protected void PluginOfTypeInvoke<T>(Action<T> action)
        {
            foreach (var plugin in _plugins.OfType<T>())
            {
                try
                {
                    action(plugin);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking plugin {plugin}: {ex}", plugin!.GetType().FullName, ex);

                }
            }
        }

        public T Plugin<T>() where T : IXrPlugin
        {
            return _plugins.OfType<T>().First();
        }

        protected virtual XrViewInfo SelectView(IList<XrViewInfo> views)
        {
            return views.First(a => a.Type == ViewConfigurationType.PrimaryStereo);
        }

        protected virtual Extent2Di SelectRenderSize(XrViewInfo view)
        {
            return view.RecommendedImageRect;
        }

        protected virtual void OnSessionChanged(SessionState state, long time)
        {
            switch (state)
            {
                case SessionState.Ready:

                    break;
            }

            _logger.LogInformation("Session state: {state}", state);

            lock (_sessionLock)
            {
                _lastSessionState = state;
                Monitor.PulseAll(_sessionLock);
            }
        }

        protected IList<string> GetSupportedExtensions()
        {
            uint propCount = 0;
            CheckResult(_xr!.EnumerateInstanceExtensionProperties((byte*)null, 0, &propCount, null), "EnumerateInstanceExtensionProperties");

            var props = new ExtensionProperties[propCount];
            for (int i = 0; i < props.Length; i++)
                props[i].Type = StructureType.ExtensionProperties;

            fixed (ExtensionProperties* pProps = props)
                CheckResult(_xr.EnumerateInstanceExtensionProperties((byte*)null, propCount, ref propCount, pProps), "EnumerateInstanceExtensionProperties");

            var result = new List<string>();
            for (int i = 0; i < props.Length; i++)
            {
                fixed (void* pExtName = props[i].ExtensionName)
                    result.Add(Marshal.PtrToStringAnsi(new nint(pExtName))!);
            }

            return result;
        }

        protected void CollectAndSelectView()
        {
            var views = new List<XrViewInfo>();

            foreach (var type in EnumerateViewConfiguration())
            {
                var props = GetViewConfigurationProperties(type);

                var blendModes = EnumerateBlendModes(type);

                var viewsConfig = EnumerateViewConfigurationView(type);
                var view = viewsConfig.First();

                views.Add(new XrViewInfo
                {
                    Type = type,
                    FovMutable = props.FovMutable != 0,
                    BlendModes = blendModes,
                    MaxImageRect = new Extent2Di
                    {
                        Width = (int)view.MaxImageRectWidth,
                        Height = (int)view.MaxImageRectHeight
                    },
                    RecommendedImageRect = new Extent2Di
                    {
                        Width = (int)view.RecommendedImageRectWidth,
                        Height = (int)view.RecommendedImageRectHeight
                    },
                    MaxSwapchainSampleCount = view.MaxSwapchainSampleCount,
                    RecommendedSwapchainSampleCount = view.RecommendedSwapchainSampleCount,
                    ViewCount = viewsConfig.Length
                });

            }

            _viewInfo = SelectView(views);

            _renderOptions = new XrRenderOptions();

            SelectRenderOptionsMode(_viewInfo, _renderOptions);
        }

        protected virtual void SelectRenderOptionsMode(XrViewInfo viewInfo, XrRenderOptions result)
        {
            EnvironmentBlendMode[] preferences = [EnvironmentBlendMode.AlphaBlend];

            result.BlendMode = preferences.First(a => viewInfo.BlendModes!.Contains(a));
            result.Size = viewInfo.RecommendedImageRect;
            result.SampleCount = viewInfo.RecommendedSwapchainSampleCount;
        }

        protected Instance CreateInstance(string appName, string engineName, IList<string> extensions)
        {
            var appInfo = new ApplicationInfo()
            {
                ApiVersion = new Version64(1, 0, 9) 
            };

            var appNameSpan = new Span<byte>(appInfo.ApplicationName, 128);
            var engNameSpan = new Span<byte>(appInfo.EngineName, 128);

            SilkMarshal.StringIntoSpan(appName, appNameSpan);
            SilkMarshal.StringIntoSpan(engineName, engNameSpan);

            var requestedExtensions = SilkMarshal.StringArrayToPtr(extensions.ToArray());

            var instanceCreateInfo = new InstanceCreateInfo
            (
                applicationInfo: appInfo,
                enabledExtensionCount: (uint)extensions.Count,
                enabledExtensionNames: (byte**)requestedExtensions
            );

            CheckResult(_xr!.CreateInstance(in instanceCreateInfo, ref _instance), "CreateInstance");

            return _instance;
        }

        protected ulong GetSystemId(FormFactor formFactor = FormFactor.HeadMountedDisplay)
        {
            var getInfo = new SystemGetInfo(formFactor: formFactor);

            CheckResult(_xr!.GetSystem(_instance, in getInfo, ref _systemId), "GetSystem");

            return _systemId;
        }


        protected SystemProperties GetSystemProperties()
        {
            var result = new SystemProperties();
            result.Type = StructureType.SystemProperties;

            CheckResult(_xr!.GetSystemProperties(_instance, _systemId, &result), "GetSystemProperties");

            return result;
        }


        protected ViewConfigurationProperties GetViewConfigurationProperties(ViewConfigurationType viewType)
        {
            var result = new ViewConfigurationProperties()
            {
                Type = StructureType.ViewConfigurationProperties
            };

            CheckResult(_xr!.GetViewConfigurationProperties(_instance, _systemId, viewType, ref result) , "GetViewConfigurationProperties");

            return result;
        }

        protected ViewConfigurationType[] EnumerateViewConfiguration()
        {
            uint count = 0;

            CheckResult(_xr!.EnumerateViewConfiguration(_instance, _systemId, 0, ref count, null), "EnumerateViewConfiguration");

            var result = new ViewConfigurationType[count];

            fixed (ViewConfigurationType* pResult = result)
                CheckResult(_xr!.EnumerateViewConfiguration(_instance, _systemId, count, ref count, pResult), "EnumerateViewConfiguration");

            return result;
        }

        protected ViewConfigurationView[] EnumerateViewConfigurationView(ViewConfigurationType viewType)
        {
            uint viewCount = 0;
            Span<ViewConfigurationView> result = stackalloc ViewConfigurationView[32];

            for (int i = 0; i < result.Length; i++)
                result[i] = new ViewConfigurationView() { Type = StructureType.ViewConfigurationView };

            CheckResult(_xr!.EnumerateViewConfigurationView(_instance, _systemId, viewType, (uint)result.Length, ref viewCount, ref result[0]), "EnumerateViewConfigurationView");

            return result[..(int)viewCount].ToArray();
        }

        protected Space CreateReferenceSpace(ReferenceSpaceType type)
        {
            return CreateReferenceSpace(type, new Posef(new Quaternionf(0f, 0f, 0f, 1f), new Vector3f(0f, 0f, 0f)));

        }
        protected Space CreateReferenceSpace(ReferenceSpaceType type, Posef pose)
        {
            var refSpace = new ReferenceSpaceCreateInfo()
            {
                Type = StructureType.ReferenceSpaceCreateInfo,
                ReferenceSpaceType = type,
                PoseInReferenceSpace = pose
            };

            Space space;
            CheckResult(_xr!.CreateReferenceSpace(_session, &refSpace, &space), "CreateReferenceSpace");
            return space;
        }

        protected void BeginSession(ViewConfigurationType viewType)
        {
            if (_sessionBegun)
                return;

            var sessionBeginInfo = new SessionBeginInfo()
            {
                Type = StructureType.SessionBeginInfo,
                PrimaryViewConfigurationType = viewType
            };

            CheckResult(_xr!.BeginSession(_session, &sessionBeginInfo), "BeginSession");

            PluginInvoke(p => p.OnSessionBegin());

            _sessionBegun = true;

        }

        protected void EndSession()
        {
            if (!_sessionBegun)
                return;

            CheckResult(_xr!.EndSession(_session), "EndSession");

            PluginInvoke(p => p.OnSessionEnd());

            _sessionBegun = false;
        }

        protected Session CreateSession()
        {
            GetSystemProperties();

            var graphic = _plugins.OfType<IXrGraphicDriver>().First();

            var binding = graphic.CreateBinding();

            var sessionInfo = new SessionCreateInfo()
            {
                Type = StructureType.SessionCreateInfo,
                SystemId = _systemId,
                Next = &binding
            };

            CheckResult(_xr!.CreateSession(Instance, &sessionInfo, ref _session), "CreateSession");

            _head = CreateReferenceSpace(ReferenceSpaceType.View);
            
            _local = CreateReferenceSpace(ReferenceSpaceType.Local);

            _stage = CreateReferenceSpace(ReferenceSpaceType.Stage);

            return _session;
        }

        protected EnvironmentBlendMode[] EnumerateBlendModes(ViewConfigurationType type)
        {
            uint count = 0;
            CheckResult(_xr!.EnumerateEnvironmentBlendModes(Instance, _systemId, type, ref count, null), "EnumerateEnvironmentBlendModes");

            var result = new EnvironmentBlendMode[count];

            fixed (EnvironmentBlendMode* pResult = result)
                CheckResult(_xr!.EnumerateEnvironmentBlendModes(Instance, _systemId, type, count, ref count, pResult), "EnumerateEnvironmentBlendModes");

            return result;
        }

        protected long[] EnumerateSwapchainFormats()
        {
            uint count = 0;
            CheckResult(_xr!.EnumerateSwapchainFormats(Session, 0, ref count, null), "EnumerateSwapchainFormats");

            var result = new long[count];

            fixed (long* pResult = result)
                CheckResult(_xr!.EnumerateSwapchainFormats(Session, count, ref count, pResult), "EnumerateSwapchainFormats");

            return result;
        }

        protected NativeArray<SwapchainImageBaseHeader> EnumerateSwapchainImages(Swapchain swapchain)
        {
            uint count = 0;

            CheckResult(_xr!.EnumerateSwapchainImages(swapchain, 0, ref count, null), "EnumerateSwapchainImages");

            var imageType = Plugin<IXrGraphicDriver>().SwapChainImageType;
            var images = new NativeArray<SwapchainImageBaseHeader>((int)count, imageType.Type!);

            for (var i = 0; i < images.Length; i++)
            {
                images.Item(i).Type = imageType.StructureType;
                images.Item(i).Next = null;
            }

            CheckResult(_xr!.EnumerateSwapchainImages(swapchain, count, ref count, images.Pointer), "EnumerateSwapchainImages");

            return images;
        }

        protected internal uint AcquireSwapchainImage(Swapchain swapchain)
        {
            var acquireInfo = new SwapchainImageAcquireInfo()
            {
                Type = StructureType.SwapchainImageAcquireInfo
            };

            uint imageIndex = 0;

            CheckResult(_xr!.AcquireSwapchainImage(swapchain, in acquireInfo, ref imageIndex), "AcquireSwapchainImage");

            return imageIndex;
        }

        protected internal void WaitSwapchainImage(Swapchain swapchain, long timeout = DurationInfinite)
        {
            var info = new SwapchainImageWaitInfo()
            {
                Type = StructureType.SwapchainImageWaitInfo,
                Timeout = timeout

            };

            CheckResult(_xr!.WaitSwapchainImage(swapchain, in info), "WaitSwapchainImage");
        }

        protected internal void ReleaseSwapchainImage(Swapchain swapchain)
        {
            var info = new SwapchainImageReleaseInfo()
            {
                Type = StructureType.SwapchainImageReleaseInfo,
            };

            CheckResult(_xr!.ReleaseSwapchainImage(swapchain, in info), "ReleaseSwapchainImage");
        }

        protected Swapchain CreateSwapChain()
        {
            if (_renderOptions == null)
                throw new ArgumentNullException("renderOptions");

            var formats = EnumerateSwapchainFormats();

            var format = Plugin<IXrGraphicDriver>().SelectSwapChainFormat(formats);

            return CreateSwapChain(_renderOptions.Size, _renderOptions.SampleCount, format);
        }


        protected Swapchain CreateSwapChain(Extent2Di size, uint sampleCount, long format)
        {
            var info = new SwapchainCreateInfo
            {
                Type = StructureType.SwapchainCreateInfo,
                Width = (uint)size.Width,
                Height = (uint)size.Height,
                Format = format,  
                ArraySize = 1,
                MipCount = 1,
                FaceCount = 1,
                SampleCount = sampleCount,
                UsageFlags = SwapchainUsageFlags.ColorAttachmentBit | SwapchainUsageFlags.SampledBit

            };

            PluginInvoke(p => p.ConfigureSwapchain(ref info));

            var result = new Swapchain();
            CheckResult(_xr!.CreateSwapchain(Session, in info, ref result), "CreateSwapchain");

            return result;
        }

        public SpaceLocation LocateSpace(Space space, Space baseSpace, long time = 0)
        {
            var result = new SpaceLocation();
            result.Type = StructureType.SpaceLocation;
            CheckResult(_xr!.LocateSpace(space, baseSpace, time, ref result), "LocateSpace");
            return result;
        }

        public ViewState LocateViews(Space space, long displayTime)
        {
            var info = new ViewLocateInfo()
            {
                Type = StructureType.ViewLocateInfo,
                Space = space,
                DisplayTime = displayTime,
                ViewConfigurationType = _viewInfo!.Type
            };

            var state = new ViewState()
            {
                Type = StructureType.ViewState
            };

            uint count = (uint)_viewInfo.ViewCount; 

            fixed (View* pViews = _views)
                CheckResult(_xr!.LocateView(_session, in info, ref state, count, ref count, pViews), "LocateView");

            return state;
        }


        public void Dispose()
        {
            if (_xr == null)
                return;

            Stop();

            _layers.Dispose();

            if (_instance.Handle != 0)
            {
                _xr.DestroyInstance(Instance);
                _instance.Handle = 0;
            }

            PluginOfTypeInvoke<IDisposable>(p => p.Dispose());

            _xr.Dispose();
            _xr = null;
            _isDisposed = true;
        }

        public bool IsStarted => _isStarted;

        public ulong SystemId => _systemId;

        public XrLayerManager Layers => _layers;

        public Instance Instance => _instance;

        public Session Session => _session;

        public Space Head => _head;

        public Space Floor => _local;

        public Space Stage => _stage;

        public SessionState SessionState => _lastSessionState;

        public XR Xr => _xr!;
    }
}
