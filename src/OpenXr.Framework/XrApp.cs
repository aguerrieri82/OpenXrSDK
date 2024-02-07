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



namespace OpenXr.Framework
{
    public struct SizeI
    {
        public int Width;

        public int Height;
    }

    public unsafe class XrApp : IDisposable
    {
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
        protected Space _head;
        protected Space _floor;
        protected Space _stage;
        protected bool _isStarted;
        protected SyncEvent _instanceReady;
        private SessionState _lastSessionState;
        private object _sessionLock;
        private bool _sessionBegun;
        protected ILogger<XrApp> _logger;

        public XrApp(params IXrPlugin[] plugins)
            : this(NullLogger<XrApp>.Instance, plugins)
        {

        }

        public XrApp(ILogger<XrApp> logger, params IXrPlugin[] plugins)
        {
            _extensions = [];
            _logger = logger;
            _plugins = plugins;
            _lastSessionState = SessionState.Unknown;
            _instanceReady = new SyncEvent();
            _sessionLock = new object();
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

        public void Start()
        {
            if (_isStarted)
                return;

            if (_xr == null)
                Initialize();



            CreateSession();

            PluginInvoke(p => p.OnSessionCreated());

            WaitForSession(SessionState.Ready, SessionState.Focused);

            BeginSession(_viewInfo!.Type);

            _isStarted = true;
        }

        protected void DisposeSpace(Space space)
        {
            if (space.Handle != 0)
            {
                CheckResult(_xr!.DestroySpace(space), "DestroySpace");
                space.Handle = 0;
            }
        }

        public void Stop()
        {
            if (!_isStarted)
                return;

            _logger.LogDebug("Stopping");

            DisposeSpace(_floor);
            DisposeSpace(_head);
            DisposeSpace(_stage);

            if (_session.Handle != 0)
            {
                CheckResult(_xr!.DestroySession(Session), "DestroySession");
                _session.Handle = 0;
            }

            _isStarted = false;
            _lastSessionState = SessionState.Unknown;
            _sessionBegun = false;

            PluginInvoke(p => p.OnSessionEnd());

            _logger.LogInformation("Stopped");

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

        public void HandleEvents()
        {
            HandleEvents(CancellationToken.None);
        }

        public void HandleEvents(CancellationToken cancellationToken)
        {
            if (!_instanceReady.Wait(cancellationToken))
                return;

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
        }

        public void WaitForSession(params SessionState[] states)
        {
            lock (_sessionLock)
            {
                while (!states.Contains(_lastSessionState))
                    Monitor.Wait(_sessionLock);
            }
        }

        protected virtual XrViewInfo SelectView(IList<XrViewInfo> views)
        {
            return views.First(a => a.Type == ViewConfigurationType.PrimaryStereo);
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

            fixed (ExtensionProperties* pProps = &props[0])
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

                foreach (var view in EnumerateViewConfigurationView(type).Take(1))
                {
                    views.Add(new XrViewInfo
                    {
                        Type = type,
                        FovMutable = props.FovMutable != 0,
                        MaxImageRect = new SizeI
                        {
                            Width = (int)view.MaxImageRectWidth,
                            Height = (int)view.MaxImageRectHeight
                        },
                        RecommendedImageRect = new SizeI
                        {
                            Width = (int)view.RecommendedImageRectWidth,
                            Height = (int)view.RecommendedImageRectHeight
                        },
                        MaxSwapchainSampleCount = view.MaxSwapchainSampleCount,
                        RecommendedSwapchainSampleCount = view.RecommendedSwapchainSampleCount
                    });
                }
            }

            _viewInfo = SelectView(views);
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

        void BeginSession(ViewConfigurationType viewType)
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

        void EndSession()
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

            var sessionInfo = new SessionCreateInfo()
            {
                Type = StructureType.SessionCreateInfo,
                SystemId = _systemId,
                Next = graphic.CreateBinding()
            };

            fixed (Session* pSession = &_session)
                CheckResult(_xr!.CreateSession(Instance, &sessionInfo, pSession), "CreateSession");

            _head = CreateReferenceSpace(ReferenceSpaceType.View);
            
            _floor = CreateReferenceSpace(ReferenceSpaceType.Local);

            _stage = CreateReferenceSpace(ReferenceSpaceType.Stage);

            return _session;
        }

        public SpaceLocation LocateSpace(Space space, Space baseSpace, long time = 0)
        {
            var result = new SpaceLocation();
            result.Type = StructureType.SpaceLocation;
            CheckResult(_xr!.LocateSpace(space, baseSpace, time, ref result), "LocateSpace");
            return result;
        }

        public void Dispose()
        {
            if (_xr == null)
                return;

            Stop();

            if (_instance.Handle != 0)
            {
                _xr.DestroyInstance(Instance);
                _instance.Handle = 0;
            }

            PluginOfTypeInvoke<IDisposable>(p => p.Dispose());

            _xr.Dispose();
            _xr = null;
        }

        public bool IsStarted => _isStarted;

        public ulong SystemId => _systemId;

        public Instance Instance => _instance;

        public Session Session => _session;

        public Space Head => _head;

        public Space Floor => _floor;

        public Space Stage => _stage;

        public SessionState SessionState => _lastSessionState;

        public XR Xr => _xr!;
    }
}
