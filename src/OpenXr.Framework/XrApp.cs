using Silk.NET.Core.Native;
using Silk.NET.Core;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;



namespace OpenXr.Framework
{
    public struct SizeI
    {
        public int Width;

        public int Height;
    }

    public unsafe class XrApp : IDisposable
    {
        protected Instance _instance;
        protected ulong _systemId;
        protected XR? _xr;
        protected List<string> _extensions;
        protected Session _session;
        protected IXrPlugin[] _plugins;
        protected SizeI _renderSize;
        protected Space _head;
        protected Space _floor;
        protected Space _stage;
        protected bool _isStarted;
        protected SyncEvent _instanceReady;
        private SessionState _lastSessionState;
        private object _sessionLock;
        private bool _sessionBegun;

        public XrApp(params IXrPlugin[] plugins)
        {
            _extensions = [];
            _plugins = plugins;
            _lastSessionState = SessionState.Unknown;
            _instanceReady = new SyncEvent();
            _sessionLock = new object();
        }

        public static bool CheckResult(Result result, string method)
        {
            if (result != Result.Success)
                throw new Exception($"{method}: {result}");
            return true;
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
                    Console.Write("WARN: {0} not supported", _extensions[i]);
                    _extensions.RemoveAt(i);
                    i--;
                }
            }

            CreateInstance(AppDomain.CurrentDomain.FriendlyName, "OpenXr.Framework", _extensions);

            GetSystemId();

            _instanceReady.Signal();
        }

        public void Start()
        {
            if (_isStarted)
                return;

            if (_xr == null)
                Initialize();

            foreach (var plugin in _plugins)
                plugin.OnInstanceCreated();

            CreateSession();

            foreach (var plugin in _plugins)
                plugin.OnSessionCreated();

            _isStarted = true;
        }

        protected void DisposeSpace(Space space)
        {
            if (space.Handle != 0)
            {
                _xr!.DestroySpace(space);
                space.Handle = 0;
            }
        }

        public void Stop()
        {
            if (!_isStarted)
                return;

            if (_session.Handle != 0)
            {
                _xr!.DestroySession(Session);
                _session.Handle = 0;
            }

            DisposeSpace(_floor);
            DisposeSpace(_head);
            DisposeSpace(_stage);

            _isStarted = false;
            _lastSessionState = SessionState.Unknown;
            _sessionBegun = false;


            foreach (var plugin in _plugins)
                plugin.OnSessionEnd();

        }

        public T Plugin<T>() where T : IXrPlugin
        {
            return _plugins.OfType<T>().First();
        }


        public void HandleEvents(CancellationToken? cancellationToken = null)
        {
            if (!_instanceReady.Wait(cancellationToken))
                return;

            EventDataBuffer buffer = new EventDataBuffer();

            while (true)
            {
                if (cancellationToken != null && cancellationToken.Value.IsCancellationRequested)
                    break;

                buffer.Type = StructureType.EventDataBuffer;

                var result = _xr!.PollEvent(_instance, &buffer);
                if (result != Result.Success)
                    break;

                Debug.WriteLine(buffer.Type);

                switch (buffer.Type)
                {
                    case StructureType.EventDataSessionStateChanged:
                        EventDataSessionStateChanged sessionChanged = *(EventDataSessionStateChanged*)&buffer;
                        OnSessionChanged(sessionChanged.State, sessionChanged.Time);
                        break;
                }

                foreach (var plugin in _plugins)
                    plugin.HandleEvent(buffer);
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

        protected void OnSessionChanged(SessionState state, long time)
        {
            switch (state)
            {
                case SessionState.Ready:
                    //BeginSession(ViewConfigurationType.PrimaryStereo);
                    break;
            }

            Debug.WriteLine("Session state: {0}", state);

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

        protected ViewConfigurationView[] GetViewConfigurationView(ViewConfigurationType viewType)
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

        public void BeginSession(ViewConfigurationType viewType)
        {
            if (_sessionBegun)
                EndSession();

            var sessionBeginInfo = new SessionBeginInfo()
            {
                Type = StructureType.SessionBeginInfo,
                PrimaryViewConfigurationType = viewType
            };

            CheckResult(_xr!.BeginSession(_session, &sessionBeginInfo), "BeginSession");

            foreach (var plugin in _plugins)
                plugin.OnSessionBegin();

            _sessionBegun = true;

        }

        public void EndSession()
        {
            if (!_sessionBegun)
                return;

            CheckResult(_xr!.EndSession(_session), "EndSession");

            foreach (var plugin in _plugins)
                plugin.OnSessionEnd();

            _sessionBegun = false;
        }

        protected Session CreateSession()
        {
            GetSystemProperties();

            var viewConfig = GetViewConfigurationView(ViewConfigurationType.PrimaryStereo).First();

            _renderSize.Height = (int)Math.Round(viewConfig.RecommendedImageRectHeight * 1.0);
            _renderSize.Width = (int)Math.Round(viewConfig.RecommendedImageRectWidth * 1.0) * 2;

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

            foreach (var plugin in _plugins.OfType<IDisposable>())
                plugin.Dispose();

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
