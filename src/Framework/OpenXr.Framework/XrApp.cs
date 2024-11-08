using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.EXT;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using XrMath;
using Action = Silk.NET.OpenXR.Action;


namespace OpenXr.Framework
{
    public enum XrAppStartMode
    {
        Render,
        Query
    }

    public enum XrAppState
    {
        Created,
        Initializing,
        Initialized,
        Starting,
        SessionCreated,
        Started,
        Stopping,
        Stopped,
        Disposed,
    }

    public unsafe class XrApp : IDisposable, IXrSession
    {
        const long DurationInfinite = 0x7fffffffffffffff;

        public bool CheckResult(Result result, string context)
        {
            if (result != Result.Success)
                throw new OpenXrException(result, context);
            //_logger.LogDebug("{context} OK", context);
            return true;
        }

        protected XR? _xr;

        protected Instance _instance;
        protected ActionSet _actionSet;
        protected Session _session;
        protected Space _head;
        protected Space _local;
        protected Space _stage;


        protected ulong _systemId;
        protected XrViewInfo? _viewInfo;
        protected SessionState _lastSessionState;
        protected View[]? _views;
        protected SystemProperties _systemProps;

        protected readonly Dictionary<HandEXT, XrHandInput> _hands = [];
        protected readonly Dictionary<string, IXrInput> _inputs = [];
        protected readonly Dictionary<string, XrHaptic> _haptics = [];
        protected readonly List<string> _extensions = [];
        protected readonly List<string> _interactionProfiles = [];
        protected readonly IXrPlugin[] _plugins;
        protected readonly ILogger _logger;
        protected readonly XrLayerManager _layers;
        protected readonly XrRenderOptions _renderOptions;
        protected readonly XrSpacesTracker _tracker;

        //TODO leave here or move?
        protected ExtPerformanceSettings? _perfSettings;
        protected internal ExtHandTracking? _handTracking;

        protected XrAppState _state;
        protected bool _isValid; //TODO rethink on _state

        public XrApp(params IXrPlugin[] plugins)
            : this(NullLogger<XrApp>.Instance, plugins)
        {
        }

        public XrApp(ILogger logger, params IXrPlugin[] plugins)
        {
            _logger = logger;
            _plugins = plugins;
            _lastSessionState = SessionState.Unknown;
            _layers = new XrLayerManager(this);
            _renderOptions = new XrRenderOptions();
            _tracker = new XrSpacesTracker(this);

            _extensions.Add(ExtPerformanceSettings.ExtensionName);
            _extensions.Add(ExtHandTracking.ExtensionName);
            _extensions.Add("XR_KHR_locate_spaces");

            Current = this;
            ReferenceFrame = Pose3.Identity;
        }

        #region START/STOP

        protected void Initialize()
        {
            _state = XrAppState.Initializing;

            try
            {
                if (_xr == null)
                {
                    _xr = XR.GetApi();

                    PluginInvoke(a => a.Initialize(this, _extensions));

                    LayersInvoke(a => a.Initialize(this, _extensions));

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
                }

                if (_instance.Handle == 0)
                {
                    CreateInstance(AppDomain.CurrentDomain.FriendlyName, "OpenXr.Framework", _extensions);

                    CreateActionSet("default", "default");

                    GetInstanceProperties();
                }

                if (_systemId == 0)
                {
                    GetSystemId();

                    PluginInvoke(p => p.OnInstanceCreated(), true);

                    _xr.TryGetInstanceExtension<ExtPerformanceSettings>(null, _instance, out _perfSettings);
                    _xr.TryGetInstanceExtension<ExtHandTracking>(null, _instance, out _handTracking);
                }

                _state = XrAppState.Initialized;
            }
            catch
            {
                _state = XrAppState.Created;
                throw;
            }
        }

        public void AttachInstance(ulong instance)
        {
            _instance.Handle = instance;
            Initialize();
        }

        public virtual void Start(XrAppStartMode mode = XrAppStartMode.Render)
        {
            if (_state != XrAppState.Created && _state != XrAppState.Stopped)
                return;

            Initialize();

            _state = XrAppState.Starting;

            CreateSession();

            CollectAndSelectView();

            SelectRenderOptions(_viewInfo, _renderOptions);

            _layers.Commit();

            LayersInvoke(a => a.Create());

            PluginInvoke(p => p.OnSessionCreated());

            _state = XrAppState.SessionCreated;

            AssertSessionCreated();

            CreateActions();

            foreach (var hand in _hands)
                hand.Value.Initialize(hand.Key);

            if (mode == XrAppStartMode.Render)
            {
                _views = CreateStructArray<View>(_viewInfo.ViewCount, StructureType.View);

                if (_perfSettings != null)
                {
                    CheckResult(_perfSettings.PerfSettingsSetPerformanceLevel(_session, PerfSettingsDomainEXT.GpuExt, _renderOptions.GpuLevel), "PerfSettingsSetPerformanceLevel");

                    CheckResult(_perfSettings.PerfSettingsSetPerformanceLevel(_session, PerfSettingsDomainEXT.CpuExt, _renderOptions.CpuLevel), "PerfSettingsSetPerformanceLevel");
                }
            }
            _state = XrAppState.Started;
        }

        protected void DestroyInstance()
        {
            foreach (var haptic in _haptics)
                haptic.Value.Destroy();

            foreach (var input in _inputs)
                input.Value.Destroy();

            /*
            foreach (var hand in _hands)
                hand.Value.Destroy();
            */

            if (_instance.Handle != 0)
            {
                CheckResult(_xr!.DestroyInstance(Instance), "DestroyInstance");
                _instance.Handle = 0;
            }

            _systemId = 0;
            _actionSet.Handle = 0;
        }

        public virtual void Stop()
        {
            _isValid = false;

            _state = XrAppState.Stopping;

            _logger.LogDebug("Stopping");

            _tracker.Clear();

            DisposeSpace(ref _local);
            DisposeSpace(ref _head);
            DisposeSpace(ref _stage);

            ListInvoke<IXrLayer>(_layers.List, p => p.Destroy());

            if (_session.Handle != 0)
            {
                CheckResult(_xr!.DestroySession(Session), "DestroySession");
                _session.Handle = 0;
            }

            DestroyInstance();

            _state = XrAppState.Stopped;
            _lastSessionState = SessionState.Unknown;

            PluginInvoke(p => p.OnSessionEnd());

            SessionChanged?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation("Stopped");
        }

        #endregion

        #region INSTANCE & SYSTEM

        protected IList<string> GetSupportedExtensions()
        {
            uint propCount = 0;
            CheckResult(_xr!.EnumerateInstanceExtensionProperties((byte*)null, 0, &propCount, null), "EnumerateInstanceExtensionProperties");

            var props = CreateStructArray<ExtensionProperties>((int)propCount, StructureType.ExtensionProperties);

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

        protected InstanceProperties GetInstanceProperties()
        {
            var props = new InstanceProperties(StructureType.InstanceProperties);

            CheckResult(_xr!.GetInstanceProperties(_instance, ref props), "GetInstanceProperties");

            return props;

        }

        protected Instance CreateInstance(string appName, string engineName, IList<string> extensions)
        {
            var appInfo = new ApplicationInfo()
            {
                ApiVersion = new Version64(1, 0, 30)
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

        public void GetSystemProperties<T>(ref T other) where T : unmanaged
        {
            fixed (T* pProps = &other)
            {
                var result = new SystemProperties
                {
                    Type = StructureType.SystemProperties,
                    Next = pProps
                };

                CheckResult(_xr!.GetSystemProperties(_instance, _systemId, &result), "GetSystemProperties");
            }
        }

        protected SystemProperties GetSystemProperties()
        {
            var result = new SystemProperties
            {
                Type = StructureType.SystemProperties
            };

            CheckResult(_xr!.GetSystemProperties(_instance, _systemId, &result), "GetSystemProperties");

            return result;
        }

        #endregion

        #region SPACE AND VIEW

        [MemberNotNull(nameof(_viewInfo))]
        protected void CollectAndSelectView()
        {
            var views = new List<XrViewInfo>();

            var swapchainFormats = EnumerateSwapchainFormats();

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
                    ViewCount = viewsConfig.Length,
                    SwapChainFormats = swapchainFormats,
                });

            }

            _viewInfo = SelectView(views);
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

        protected ViewConfigurationProperties GetViewConfigurationProperties(ViewConfigurationType viewType)
        {
            var result = new ViewConfigurationProperties()
            {
                Type = StructureType.ViewConfigurationProperties
            };

            CheckResult(_xr!.GetViewConfigurationProperties(_instance, _systemId, viewType, ref result), "GetViewConfigurationProperties");

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

        protected ViewState LocateViews(Space space, long displayTime)
        {
            return LocateViews(space, displayTime, _views!);
        }

        public unsafe ViewState LocateViews(Space space, long displayTime, View[] views)
        {
            Debug.Assert(_viewInfo != null);

            var info = new ViewLocateInfo()
            {
                Type = StructureType.ViewLocateInfo,
                Space = space,
                DisplayTime = displayTime,
                ViewConfigurationType = _viewInfo.Type
            };

            var state = new ViewState()
            {
                Type = StructureType.ViewState
            };

            uint count = (uint)views.Length;

            fixed (View* pViews = views)
                CheckResult(_xr!.LocateView(_session, in info, ref state, count, ref count, pViews), "LocateView");


            return state;
        }


        public XrSpaceLocation LocateSpace(Space space, Space baseSpace, long time = 0)
        {
            var result = new SpaceLocation();
            result.Type = StructureType.SpaceLocation;
            CheckResult(_xr!.LocateSpace(space, baseSpace, time, ref result), "LocateSpace");
            return new XrSpaceLocation
            {
                Pose = ReferenceFrame.Multiply(result.Pose.ToPose3()),
                Flags = result.LocationFlags
            };
        }

        [Obsolete("Oculus throws access violation")]
        public unsafe XrSpaceLocation[] LocateSpaces(Space[] spaces, Space baseSpace, long time = 0)
        {
            var locations = new SpaceLocationData[spaces.Length];

            fixed (Space* pSpaces = spaces)
            fixed (SpaceLocationData* pLocations = locations)
            {
                var result = new SpaceLocations
                {
                    Type = StructureType.SpaceLocations,
                    Locations = pLocations,
                    Next = null,
                    LocationCount = (uint)locations.Length
                };

                var info = new SpacesLocateInfo
                {
                    Type = StructureType.SpacesLocateInfo,
                    BaseSpace = baseSpace,
                    Spaces = pSpaces,
                    SpaceCount = (uint)spaces.Length,
                    Time = time,
                    Next = null,
                };

                CheckResult(_xr!.LocateSpaces(_session, &info, &result), "LocateSpaces");
            }

            return locations.Select(a => new XrSpaceLocation
            {
                Pose = a.Pose.ToPose3().Multiply(ReferenceFrame),
                Flags = a.LocationFlags
            }).ToArray();
        }

        protected void DisposeSpace(ref Space space)
        {
            if (space.Handle != 0)
            {
                CheckResult(_xr!.DestroySpace(space), "DestroySpace");
                space.Handle = 0;
            }
        }

        #endregion

        #region SESSION


        [MemberNotNull(nameof(_renderOptions))]
        [MemberNotNull(nameof(_xr))]
        [MemberNotNull(nameof(_viewInfo))]
        protected void AssertSessionCreated()
        {
            if (_session.Handle == 0 || _renderOptions == null || _xr == null || _viewInfo == null)
                throw new InvalidOperationException();
        }

        protected void BeginSession(ViewConfigurationType viewType)
        {
            var sessionBeginInfo = new SessionBeginInfo()
            {
                Type = StructureType.SessionBeginInfo,
                PrimaryViewConfigurationType = viewType
            };

            CheckResult(_xr!.BeginSession(_session, &sessionBeginInfo), "BeginSession");

            PluginInvoke(p => p.OnSessionBegin());

            _isValid = true;
        }

        protected void EndSession()
        {
            CheckResult(_xr!.EndSession(_session), "EndSession");

            PluginInvoke(p => p.OnSessionEnd());

            _isValid = false;
        }

        protected Session CreateSession()
        {
            _systemProps = GetSystemProperties();

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

            _tracker.Add(_head, TimeSpan.Zero);

            return _session;
        }

        protected virtual void OnSessionChanged(SessionState state, long time)
        {
            _lastSessionState = state;

            _logger.LogInformation("Session state: {state}", state);

            switch (state)
            {
                case SessionState.Ready:
                    BeginSession(_viewInfo!.Type);
                    break;
                case SessionState.Stopping:
                    EndSession();
                    break;
                case SessionState.Exiting:
                    Dispose();
                    break;
                case SessionState.LossPending:
                    //TODO handle
                    break;
            }

            SessionChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region SWAPCHAIN

        public void DestroySwapchain(Swapchain swapchain)
        {
            CheckResult(_xr!.DestroySwapchain(swapchain), "DestroySwapchain");
        }

        public long[] EnumerateSwapchainFormats()
        {
            uint count = 0;
            CheckResult(_xr!.EnumerateSwapchainFormats(Session, 0, ref count, null), "EnumerateSwapchainFormats");

            var result = new long[count];

            fixed (long* pResult = result)
                CheckResult(_xr!.EnumerateSwapchainFormats(Session, count, ref count, pResult), "EnumerateSwapchainFormats");

            return result;
        }

        public NativeArray<SwapchainImageBaseHeader> EnumerateSwapchainImages(Swapchain swapchain)
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

        public uint AcquireSwapchainImage(Swapchain swapchain)
        {
            var acquireInfo = new SwapchainImageAcquireInfo()
            {
                Type = StructureType.SwapchainImageAcquireInfo
            };

            uint imageIndex = 0;

            CheckResult(_xr!.AcquireSwapchainImage(swapchain, in acquireInfo, ref imageIndex), "AcquireSwapchainImage");

            return imageIndex;
        }

        public void WaitSwapchainImage(Swapchain swapchain, long timeout = DurationInfinite)
        {
            var info = new SwapchainImageWaitInfo()
            {
                Type = StructureType.SwapchainImageWaitInfo,
                Timeout = timeout

            };

            CheckResult(_xr!.WaitSwapchainImage(swapchain, in info), "WaitSwapchainImage");
        }

        public void ReleaseSwapchainImage(Swapchain swapchain)
        {
            var info = new SwapchainImageReleaseInfo()
            {
                Type = StructureType.SwapchainImageReleaseInfo,
            };

            CheckResult(_xr!.ReleaseSwapchainImage(swapchain, in info), "ReleaseSwapchainImage");
        }

        protected internal Swapchain CreateSwapChain(bool isDepth = false)
        {
            if (_renderOptions == null)
                throw new ArgumentNullException("renderOptions");

            var size = _renderOptions.Size;

            if (_renderOptions.RenderMode == XrRenderMode.Stereo)
                size.Width *= 2;

            var arraySize = (uint)(_renderOptions.RenderMode == XrRenderMode.MultiView ? 2 : 1);

            var format = isDepth ? _renderOptions.DepthFormat : _renderOptions.ColorFormat;

            var usage = (isDepth ? SwapchainUsageFlags.DepthStencilAttachmentBit : SwapchainUsageFlags.ColorAttachmentBit);/* | SwapchainUsageFlags.SampledBit;*/

            return CreateSwapChain(size, format, arraySize, usage);
        }

        public Swapchain CreateSwapChain(Extent2Di size, long format, uint arraySize, SwapchainUsageFlags usage, bool mainSwapChain = true)
        {
            var info = new SwapchainCreateInfo
            {
                Type = StructureType.SwapchainCreateInfo,
                Width = (uint)size.Width,
                Height = (uint)size.Height,
                Format = format,
                ArraySize = arraySize,
                MipCount = 1,
                FaceCount = 1,
                SampleCount = 1,
                UsageFlags = usage
            };

            if (mainSwapChain)
                PluginInvoke(p => p.ConfigureSwapchain(ref info));

            var result = new Swapchain();
            CheckResult(_xr!.CreateSwapchain(Session, in info, ref result), "CreateSwapchain");

            return result;
        }

        #endregion

        #region FRAMES

        protected void TrySyncActions(Space space, long predictedDisplayTime)
        {
            if (_actionSet.Handle == 0 || _lastSessionState != SessionState.Focused)
                return;

            try
            {
                SyncActions();

                ListInvoke<IXrInput>(_inputs.Values, a => a.Update(space, predictedDisplayTime));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Actions Sync failed: {ex}", ex.Message);
            }
        }

        protected bool IsSessionReady()
        {
            return _lastSessionState == SessionState.Ready ||
                   _lastSessionState == SessionState.Focused ||
                   _lastSessionState == SessionState.Visible ||
                   _lastSessionState == SessionState.Synchronized;
        }

        public bool RenderFrame(Space space)
        {
            AssertSessionCreated();

            PoolEvents();

            if (!_isValid)
            {
                _logger.LogWarning("Invalid state, waiting...");
                Thread.Sleep(100);
                return false;
            }

            var state = WaitFrame();

            var frameTime = state.PredictedDisplayTime;

            FramePredictedDisplayTime = frameTime;

            _tracker.Update(space, frameTime);

            TrySyncActions(space, frameTime);

            foreach (var hand in _hands)
                hand.Value.LocateHandJoints(space, frameTime);

            BeginFrame();

            ListInvoke<IXrLayer>(_layers.List, a => a.OnBeginFrame(space, frameTime));

            CompositionLayerBaseHeader*[]? layers = null;

            uint layerCount = 0;

            try
            {
                if (state.ShouldRender != 0)
                {
                    var viewsState = LocateViews(space, frameTime);


                    var isPosValid = (viewsState.ViewStateFlags & ViewStateFlags.OrientationValidBit) != 0 &&
                                     (viewsState.ViewStateFlags & ViewStateFlags.PositionValidBit) != 0;

                    if (isPosValid)
                        layers = _layers.Render(ref _views!, space, frameTime, out layerCount);
                }
            }
            finally
            {
                ListInvoke<IXrLayer>(_layers.List, a => a.OnEndFrame());
                EndFrame(frameTime, ref layers, layerCount);

                FramePredictedDisplayTime = 0;
            }

            return true;
        }

        protected void UpdateActions()
        {

        }

        protected void BeginFrame()
        {
            var info = new FrameBeginInfo()
            {
                Type = StructureType.FrameBeginInfo,
            };
            CheckResult(_xr!.BeginFrame(_session, in info), "BeginFrame");
        }

        protected FrameState WaitFrame()
        {
            var info = new FrameWaitInfo()
            {
                Type = StructureType.FrameWaitInfo,
            };

            var result = new FrameState
            {
                Type = StructureType.FrameState
            };
            CheckResult(_xr!.WaitFrame(_session, in info, ref result), "WaitFrame");
            return result;
        }


        protected void EndFrame(long displayTime, ref CompositionLayerBaseHeader*[]? layers, uint count)
        {
            AssertSessionCreated();

            fixed (CompositionLayerBaseHeader** pLayers = layers)
            {
                var frameEndInfo = new FrameEndInfo()
                {
                    Type = StructureType.FrameEndInfo,
                    EnvironmentBlendMode = _renderOptions.BlendMode,
                    DisplayTime = displayTime,
                    LayerCount = count,
                    Layers = pLayers
                };

                CheckResult(_xr.EndFrame(_session, in frameEndInfo), "EndFrame");
            }
        }

        #endregion

        #region HANDS

        public XrHandInput AddHand(HandEXT type)
        {
            return AddHand<XrHandInput>(type);
        }

        public T AddHand<T>(HandEXT type) where T : XrHandInput
        {
            if (_hands.TryGetValue(type, out var value))
                return (T)value;

            var instance = (T)Activator.CreateInstance(typeof(T), this)!;
            if (_session.Handle != 0)
                instance.Initialize(type);

            _hands[type] = instance;
            return instance;
        }


        #endregion

        #region ACTIONS

        public void AddActions(IXrActionBuilder builder)
        {
            foreach (var item in builder.Inputs)
                AddInput(item);

            foreach (var item in builder.Haptics)
                AddHaptic(item);

            foreach (var item in builder.Profiles)
                _interactionProfiles.Add(item);

        }

        public object WithInteractionProfile(Type type, Action<IXrActionBuilder> build)
        {
            var builderType = typeof(XrActionsBuilder<>).MakeGenericType(type);

            var builder = (IXrActionBuilder)Activator.CreateInstance(builderType, this)!;

            build(builder);

            AddActions(builder);

            return builder.Result;
        }

        public T WithInteractionProfile<T>(Action<XrActionsBuilder<T>> build) where T : IXrBasicInteractionProfile, new()
        {
            var builder = new XrActionsBuilder<T>(this);

            build(builder);

            AddActions(builder);

            return builder.Result;
        }


        public XrInput<T> AddInput<T>(string path, string name)
        {
            var input = XrInput<T>.Create(this, path, name);

            AddInput(input);

            return input;
        }

        protected void AddInput(IXrInput input)
        {
            if (_state == XrAppState.SessionCreated || _state == XrAppState.Started)
                input.Initialize();

            _inputs[input.Name] = input;
        }

        protected void AddHaptic(XrHaptic haptic)
        {
            if (_state == XrAppState.SessionCreated || _state == XrAppState.Started)
                haptic.Initialize();

            _haptics[haptic.Name] = haptic;
        }


        protected void CreateActions()
        {
            var suggBindings = new List<ActionSuggestedBinding>();

            foreach (var input in _inputs.Values.Cast<IXrAction>().Union(_haptics.Values))
            {
                try
                {
                    var res = input.Initialize();
                    suggBindings.Add(res);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to create input '{input}': {ex}", input.Name, ex);
                }
            }


            if (suggBindings.Count > 0)
            {
                foreach (var profile in _interactionProfiles)
                {
                    try
                    {
                        SuggestInteractionProfileBindings(StringToPath(profile), suggBindings.ToArray());
                        break;
                    }
                    catch (OpenXrException ex)
                    {
                        _logger.LogWarning($"Interaction profile not supported ({ex.Result}): {profile}");
                    }
                }
            }
            AttachSessionToActionSet();
        }

        protected ActionSet CreateActionSet(string name, string localizedName)
        {
            var info = new ActionSetCreateInfo()
            {
                Type = StructureType.ActionSetCreateInfo,
                Priority = 0
            };

            var nameSpan = new Span<byte>(info.ActionSetName, 64);
            var localizedNameSpan = new Span<byte>(info.LocalizedActionSetName, 128);

            SilkMarshal.StringIntoSpan(name, nameSpan);
            SilkMarshal.StringIntoSpan(localizedName, localizedNameSpan);

            CheckResult(_xr!.CreateActionSet(_instance, in info, ref _actionSet), "CreateActionSet");

            return _actionSet;
        }

        protected internal Action CreateAction(string name, string localizedName, ActionType type, params ulong[] paths)
        {
            var info = new ActionCreateInfo
            {
                Type = StructureType.ActionCreateInfo,
                ActionType = type,
                CountSubactionPaths = (uint)paths.Length,
            };

            var nameSpan = new Span<byte>(info.ActionName, 64);
            var localizedNameSpan = new Span<byte>(info.LocalizedActionName, 128);

            SilkMarshal.StringIntoSpan(name.ToLower(), nameSpan);
            SilkMarshal.StringIntoSpan(localizedName, localizedNameSpan);
            var result = new Action();

            if (paths.Length > 0)
            {
                fixed (ulong* pPaths = paths)
                {
                    info.SubactionPaths = pPaths;
                    CheckResult(_xr!.CreateAction(_actionSet, in info, ref result), "CreateAction");
                }
            }
            else
                CheckResult(_xr!.CreateAction(_actionSet, in info, ref result), "CreateAction");

            return result;
        }

        protected internal ulong StringToPath(string path)
        {
            ulong result = 0;

            CheckResult(_xr!.StringToPath(_instance, path, ref result), "StringToPath");

            return result;
        }

        protected internal void SuggestInteractionProfileBindings(ulong ipPath, ActionSuggestedBinding[] bindings)
        {

            fixed (ActionSuggestedBinding* pBindings = bindings)
            {
                var info = new InteractionProfileSuggestedBinding
                {
                    Type = StructureType.InteractionProfileSuggestedBinding,
                    InteractionProfile = ipPath,
                    CountSuggestedBindings = (uint)bindings.Length,
                    SuggestedBindings = pBindings
                };

                CheckResult(_xr!.SuggestInteractionProfileBinding(_instance, in info), "SuggestInteractionProfileBinding");
            }
        }

        protected void AttachSessionToActionSet()
        {
            fixed (ActionSet* pSet = &_actionSet)
            {
                var info = new SessionActionSetsAttachInfo()
                {
                    Type = StructureType.SessionActionSetsAttachInfo,
                    ActionSets = pSet,
                    CountActionSets = 1
                };

                CheckResult(_xr!.AttachSessionActionSets(_session, in info), "AttachSessionActionSets");
            }
        }

        protected void SyncActions()
        {
            if (_actionSet.Handle == 0)
                return;

            var activeActionSet = new ActiveActionSet
            {
                ActionSet = _actionSet,
                SubactionPath = 0
            };

            var info = new ActionsSyncInfo
            {
                Type = StructureType.ActionsSyncInfo,
                CountActiveActionSets = 1,
                ActiveActionSets = &activeActionSet
            };

            CheckResult(_xr!.SyncAction(_session, in info), "SyncAction");
        }

        protected internal Space CreateActionSpace(Action action, ulong subPath)
        {
            var info = new ActionSpaceCreateInfo()
            {
                Type = StructureType.ActionSpaceCreateInfo,
                Action = action,
                SubactionPath = subPath,
            };
            info.PoseInActionSpace.Orientation.W = 1;
            var result = new Space();
            CheckResult(_xr!.CreateActionSpace(_session, in info, ref result), "CreateActionSpace");
            return result;
        }


        protected internal ActionStateFloat GetActionStateFloat(Action action, ulong subActionPath = 0)
        {
            var info = new ActionStateGetInfo
            {
                Type = StructureType.ActionStateGetInfo,
                Action = action,
                SubactionPath = subActionPath,
            };

            var state = new ActionStateFloat(StructureType.ActionStateFloat);

            CheckResult(_xr!.GetActionStateFloat(_session, in info, ref state), "GetActionStateFloat");

            return state;
        }

        protected internal ActionStateVector2f GetActionStateVector2(Action action, ulong subActionPath = 0)
        {
            var info = new ActionStateGetInfo
            {
                Type = StructureType.ActionStateGetInfo,
                Action = action,
                SubactionPath = subActionPath,
            };

            var state = new ActionStateVector2f(StructureType.ActionStateVector2f);

            CheckResult(_xr!.GetActionStateVector2(_session, in info, ref state), "GetActionStateVector2");

            return state;
        }

        protected internal ActionStateBoolean GetActionStateBoolean(Action action, ulong subActionPath = 0)
        {
            var info = new ActionStateGetInfo
            {
                Type = StructureType.ActionStateGetInfo,
                Action = action,
                SubactionPath = subActionPath,
            };

            var state = new ActionStateBoolean(StructureType.ActionStateBoolean);

            CheckResult(_xr!.GetActionStateBoolean(_session, in info, ref state), "ActionStateBoolean");

            return state;
        }

        protected internal bool GetActionPoseIsActive(Action action, ulong subActionPath = 0)
        {
            var info = new ActionStateGetInfo
            {
                Type = StructureType.ActionStateGetInfo,
                Action = action,
                SubactionPath = subActionPath,
            };


            var state = new ActionStatePose(StructureType.ActionStatePose);

            CheckResult(_xr!.GetActionStatePose(_session, in info, ref state), "ActionStatePose");

            return state.IsActive != 0;
        }

        protected internal void ApplyVibrationFeedback(Action action, float frequencyHz, float amplitude, TimeSpan duration, ulong subActionPath = 0)
        {
            if (_xr == null)
                return;

            var info = new HapticActionInfo(StructureType.HapticActionInfo)
            {
                Action = action,
                SubactionPath = subActionPath
            };

            var vibration = new HapticVibration(StructureType.HapticVibration)
            {
                Duration = (long)duration.TotalNanoseconds,
                Amplitude = amplitude,
                Frequency = frequencyHz
            };

            CheckResult(_xr!.ApplyHapticFeedback(_session, in info, (HapticBaseHeader*)&vibration), "ApplyHapticFeedback");
        }

        protected internal void StopHapticFeedback(Action action, ulong subActionPath = 0)
        {
            if (_xr == null)
                return;

            var info = new HapticActionInfo(StructureType.HapticActionInfo)
            {
                Action = action,
                SubactionPath = subActionPath
            };

            CheckResult(_xr!.StopHapticFeedback(_session, in info), "StopHapticFeedback");
        }


        #endregion

        #region EVENTS

        public bool PoolEvents()
        {
            var buffer = new EventDataBuffer();

            while (_xr != null)
            {
                buffer.Type = StructureType.EventDataBuffer;

                try
                {
                    var result = _xr.PollEvent(_instance, ref buffer);
                    if (result != Result.Success)
                        return false;


                    _logger.LogDebug("New event {ev}", buffer.Type);

                    switch (buffer.Type)
                    {
                        case StructureType.EventDataSessionStateChanged:
                            var sessionChanged = buffer.Convert().To<EventDataSessionStateChanged>();
                            _session = sessionChanged.Session;
                            OnSessionChanged(sessionChanged.State, sessionChanged.Time);

                            break;
                        case StructureType.EventDataInstanceLossPending:
                            //TODO handle
                            break;
                        case StructureType.EventDataReferenceSpaceChangePending:
                            //TODO handle
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

        #endregion

        #region UTILS

        protected static T[] CreateStructArray<T>(int count, StructureType type) where T : unmanaged
        {
            var result = new T[count];
            fixed (T* pResult = result)
            {
                while (count-- > 0)
                    ((BaseInStructure*)&pResult[count])->Type = type;
            }
            return result;
        }

        protected void LayersInvoke(Action<IXrLayer> action)
        {
            //Prioriry projection layer
            ListInvoke(_layers.List.OrderBy(a => a is XrProjectionLayer ? 0 : 1), action);
        }

        protected void PluginInvoke(Action<IXrPlugin> action, bool mustSucceed = false)
        {
            ListInvoke(_plugins, action, mustSucceed);
        }

        protected void ListInvoke<T>(IEnumerable items, Action<T> action, bool mustSucceed = false)
        {
            Exception? lastEx = null;

            foreach (var plugin in items.OfType<T>())
            {
                try
                {
                    action(plugin);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking plugin {plugin}: {ex}", plugin!.GetType().FullName, ex);
                    lastEx = ex;
                }
            }
            if (lastEx != null && mustSucceed)
                throw lastEx;
        }

        public T Plugin<T>() where T : IXrPlugin
        {
            return _plugins.OfType<T>().First();
        }

        #endregion

        #region CUSTOMIZATION

        protected virtual XrViewInfo SelectView(IList<XrViewInfo> views)
        {
            return views.First(a => a.Type == ViewConfigurationType.PrimaryStereo);
        }

        protected virtual void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {
            Debug.Assert(viewInfo.BlendModes != null);

            EnvironmentBlendMode[] preferences = [EnvironmentBlendMode.AlphaBlend, EnvironmentBlendMode.Opaque];

            result.BlendMode = preferences.First(a => viewInfo.BlendModes.Contains(a));
            result.Size = viewInfo.RecommendedImageRect;
            //TODO change this
            //result.SampleCount = viewInfo.RecommendedSwapchainSampleCount;

            result.Size = new Extent2Di
            {
                Height = (int)(result.Size.Height * _renderOptions.ResolutionScale),
                Width = (int)(result.Size.Width * _renderOptions.ResolutionScale),
            };

            PluginInvoke(a => a.SelectRenderOptions(viewInfo, result));
        }

        #endregion

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

            ListInvoke<IDisposable>(_plugins, p => p.Dispose());

            _xr.Dispose();
            _xr = null;
            _state = XrAppState.Disposed;

            Current = null;

            GC.SuppressFinalize(this);
        }

        protected internal XrViewInfo? ViewInfo => _viewInfo;

        public XrAppState State => _state;

        public bool IsStarted => _state == XrAppState.Started;

        public ulong SystemId => _systemId;

        public XrRenderOptions RenderOptions => _renderOptions;

        public SystemProperties SystemProps => _systemProps;

        public XrLayerManager Layers => _layers;

        public Instance Instance => _instance;

        public Session Session => _session;

        public Space Head => _head;

        public Space Local => _local;

        public Space Stage => _stage;

        public Space ReferenceSpace => UseLocalSpace ? Local : Stage;

        public SessionState SessionState => _lastSessionState;

        public ILogger Logger => _logger;

        public XrSpacesTracker SpacesTracker => _tracker;

        public IReadOnlyDictionary<string, IXrInput> Inputs => _inputs;

        public IReadOnlyDictionary<string, XrHaptic> Haptics => _haptics;

        public IReadOnlyDictionary<HandEXT, XrHandInput> Hands => _hands;

        public XR Xr => _xr ?? throw new InvalidOperationException("App not initialized");

        public event EventHandler? SessionChanged;

        public static XrApp? Current { get; internal set; }

        public long FramePredictedDisplayTime { get; set; }

        public Pose3 ReferenceFrame { get; set; }

        public bool UseLocalSpace { get; set; } 
    }
}
