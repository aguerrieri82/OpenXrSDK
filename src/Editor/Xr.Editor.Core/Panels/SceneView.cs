using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System.Diagnostics.CodeAnalysis;
using Xr.Engine;
using Xr.Engine.Filament;
using Xr.Engine.OpenGL;
using Xr.Engine.OpenXr;

namespace Xr.Editor
{
    public class SceneViewState : IObjectState
    {
        public ObjectId Camera;

        public ObjectId Scene;

        public Dictionary<string, object>? Tools;
    }

    public enum SceneXrState
    {
        None,
        StartRequested,
        StopRequested
    }

    public class SceneView : BasePanel, IStateManager<SceneViewState>
    {
        protected Camera? _camera;
        protected Scene? _scene;
        protected Thread? _renderThread;
        protected bool _isStarted;
        protected Rect2I _view = new();
        protected readonly IRenderSurface _renderSurface;
        protected XrApp? _xrApp;
        protected XrOculusTouchController? _inputs;
        protected bool _isXrActive;
        protected List<IEditorTool> _tools = [];
        protected SceneXrState _xrState;
        private IRenderEngine _render;

        public SceneView(IRenderSurface renderSurface)
        {
            _renderSurface = renderSurface;
            _renderSurface.SizeChanged += OnSizeChanged;
            _renderSurface.Ready += OnSurfaceReady;

            AddTool(new PickTool());
            AddTool(new OrbitTool());
        }

        [MemberNotNull(nameof(_xrApp))]
        protected void CreateXrApp()
        {
            var options = new OculusXrPluginOptions
            {
                EnableMultiView = false,
                SampleCount = 1,
                ResolutionScale = 1f
            };

            _xrApp = new XrApp(NullLogger.Instance,
                     new XrGraphicDriver(_renderSurface),
                     new OculusXrPlugin(options));

            _inputs = _xrApp.WithInteractionProfile<XrOculusTouchController>(bld => bld
               .AddAction(a => a.Right!.Button!.AClick)
               .AddAction(a => a.Right!.GripPose)
               .AddAction(a => a.Right!.AimPose)
               .AddAction(a => a.Right!.Haptic!)
               .AddAction(a => a.Right!.TriggerValue!)
               .AddAction(a => a.Right!.SqueezeValue!)
               .AddAction(a => a.Right!.TriggerClick));

            _scene!.AddComponent(new RayCollider(_inputs.Right!.AimPose!));
            _scene.AddComponent(new ObjectGrabber(
                _inputs.Right!.GripPose!,
                _inputs.Right!.Haptic!,
                _inputs.Right!.SqueezeValue!,
                _inputs.Right!.TriggerValue!));

            _xrApp.Layers.Add<XrPassthroughLayer>();

            _xrApp.BindEngineApp(_scene!.App!, options.SampleCount, options.EnableMultiView);

            _xrApp.StartEventLoop(() => !_isStarted);
        }

        protected void OnSizeChanged(object? sender, EventArgs e)
        {
            UpdateSize();
        }

        protected void OnSurfaceReady(object? sender, EventArgs e)
        {
            UpdateSize();
            Start();
        }

        public void StartXr()
        {
            _render.ReleaseContext(true);
            _renderSurface.TakeContext();

            if (_xrApp == null)
                CreateXrApp();

            try
            {
                _xrApp!.Start();
                _xrState = SceneXrState.StartRequested;
                _ui.NotifyMessage("XR Session started", MessageType.Info);

            }
            catch (Exception ex)
            {
                IsXrActive = false;
                _ui.NotifyMessage(ex.Message, MessageType.Error);
                _xrApp.Dispose();
                _xrApp = null;
            }
            finally
            {
                _renderSurface.ReleaseContext();
                _render.ReleaseContext(false);
            }

            OnPropertyChanged(nameof(IsXrActive));
        }

        public void StopXr()
        {
            try
            {
                _xrApp?.Stop();
                _xrState = SceneXrState.StopRequested;
                _ui.NotifyMessage("XR Session stopped", MessageType.Info);
            }
            catch (Exception ex)
            {
                _xrApp?.Dispose();
                _xrApp = null;
                _ui.NotifyMessage(ex.Message, MessageType.Error);

            }
        }

        protected void RenderLoop()
        {
            _render = _renderSurface.CreateRenderEngine();

            if (_scene?.App != null)
                _scene.App.Renderer = _render;


            while (_isStarted)
            {
                if (_scene?.App == null)
                    Thread.Sleep(50);
                else
                {
                    if (_isXrActive && _xrState != SceneXrState.StartRequested)
                    {
                        StartXr();
                        _renderSurface.EnableVSync(false); 
                    }

                    if (!_isXrActive && _xrApp != null && _xrState != SceneXrState.StopRequested)
                    {
                        StopXr();
                        _renderSurface.EnableVSync(true);
             
                    }

                    _renderSurface.ReleaseContext();

                    if (_xrApp != null && _xrApp.IsStarted)
                    {
                        _xrApp.RenderFrame(_xrApp.Stage);
                        _render.SetDefaultRenderTarget();
                        _render.Render(_scene!, _camera!, _view);
                    }
                    else
                        _scene.App!.RenderFrame(_view);

                    _renderSurface.SwapBuffers();

                    OnPropertyChanged(nameof(Stats));
                }
            }
        }

        protected void UpdateSize()
        {
            _view.Width = (uint)(_renderSurface!.Size.X);
            _view.Height = (uint)(_renderSurface.Size.Y);

            if (_camera is PerspectiveCamera persp)
                persp.SetFov(45, _view.Width, _view.Height);
        }

        protected void Start()
        {
            if (_isStarted)
                return;

            _isStarted = true;

            //_renderSurface!.ReleaseContext();

            _renderThread = new Thread(RenderLoop);

            _renderThread.Start();
        }

        protected void Stop()
        {
            if (!_isStarted)
                return;
            _isStarted = false;
            _renderThread!.Join();
            _renderThread = null;
        }

        public override Task CloseAsync()
        {
            StopXr();
            
            Stop();

            return base.CloseAsync();
        }

        public Scene? Scene
        {
            get => _scene;
            set
            {
                if (_scene == value)
                    return;

                _scene = value;

                Camera = _scene?.ActiveCamera;

                OnPropertyChanged(nameof(Scene));
                OnPropertyChanged(nameof(CameraList));
            }
        }

        public Camera? Camera
        {
            get => _camera;
            set
            {
                if (_camera == value)
                    return;
                _camera = value;
                OnPropertyChanged(nameof(Camera));
            }
        }

        public bool IsXrActive
        {
            get => _isXrActive;
            set
            {
                if (value == _isXrActive)
                    return;

                _isXrActive = value;
                
                if (!_isXrActive && _xrApp != null)
                    _xrApp.RequestStop();

                OnPropertyChanged(nameof(IsXrActive));
            }
        }

        public T AddTool<T>(T tool) where T : IEditorTool
        {
            _tools.Add(tool);

            tool.Attach(this);

            return tool;
        }

        public SceneViewState GetState(StateContext ctx)
        {
            throw new NotImplementedException();
        }

        public void SetState(SceneViewState state, StateContext ctx)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<IEditorTool> Tools => _tools;

        public EngineAppStats? Stats => _scene?.App?.Stats;

        public IRenderSurface RenderSurface => _renderSurface;

        public IEnumerable<Camera> CameraList => _scene?.Descendants<Camera>() ?? [];
    }
}
