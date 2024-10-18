
using OpenXr.Framework.Oculus;
using System.Xml.Linq;
using XrEditor.Services;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Services;
using XrInteraction;
using XrMath;



namespace XrEditor
{
    public enum SceneXrState
    {
        None,
        StartRequested,
        StopRequested
    }

    class RayPointerHost : Behavior<Scene3D>, IRayPointer
    {
        readonly IRayPointer _pointer;

        public RayPointerHost(IRayPointer pointer)
        {
            _pointer = pointer;
        }

        public RayPointerStatus GetPointerStatus()
        {
            return _pointer.GetPointerStatus();
        }

        public void CapturePointer()
        {
            _pointer.CapturePointer();
        }

        public void ReleasePointer()
        {
            _pointer.ReleasePointer();
        }

        public string Name => _pointer.Name;

        public int PointerId => _pointer.PointerId;

        public bool IsCaptured => _pointer.IsCaptured;
    }

    public class SceneView : BasePanel
    {
        protected readonly IRenderSurface _renderSurface;


        protected Camera? _camera;
        protected Scene3D? _scene;
        protected Thread? _renderThread;
        protected bool _isStarted;
        protected Rect2I _view = new();
        protected XrHandInputMesh? _rHand;
        protected XrOculusTouchController? _inputs;
        protected List<IEditorTool> _tools = [];
        protected SceneXrState _xrState;
        protected IRenderEngine? _render;
        protected XrEngineApp? _engine;
        protected ToolbarView _toolbar;
        protected MemoryStateContainer? _sceneState;
        protected readonly QueueDispatcher _renderDispatcher;
        protected readonly ActionView _playButton;
        protected readonly ActionView _pauseButton;
        protected readonly ActionView _stopButton;
        protected readonly ActionView _xrButton;
        protected readonly TextView _fpsLabel;

        public SceneView(IRenderSurface renderSurface)
        {
            _renderSurface = renderSurface;
            _renderSurface.SizeChanged += OnSizeChanged;
            _renderSurface.Ready += OnSurfaceReady;
            _toolbar = new ToolbarView();
            _renderDispatcher = new QueueDispatcher();
            _xrButton = _toolbar.AddToggle("icon_visibility", value =>
            {
                if (value)
                    StartXr();
                else
                    StopXr();
            });

            _fpsLabel = _toolbar.AddText(string.Empty);
            _toolbar.AddDivider();
            _playButton = _toolbar.AddButton("icon_play_arrow", () => StartApp());
            _pauseButton = _toolbar.AddButton("icon_pause", () => PauseApp());
            _stopButton = _toolbar.AddButton("icon_stop", () => StopApp());
        }

        protected async Task CreateAppAsync()
        {
            _engine = EditorDebug.CreateApp();

            _engine.App.ActiveScene!.AddComponent(new RayPointerHost(_tools.OfType<PickTool>().Single()));

            await _main.ExecuteAsync(() =>
            {
                Scene = _engine.App.ActiveScene!;
                Context.Require<SelectionManager>().Set(Scene.GetNode());
                UpdateControls();
                if (EditorDebug.AutoStartApp)
                    StartApp();
            });
        }

        protected void OnSizeChanged(object? sender, EventArgs e)
        {
            UpdateSize();
        }

        protected void OnSurfaceReady(object? sender, EventArgs e)
        {
            AddTool(new SelectionTool());
            AddTool(new OrbitTool());
            UpdateSize();
            Start();
        }

        protected void UpdateControls()
        {
            _playButton.IsActive = _engine!.App.PlayState == PlayState.Start;
            _stopButton.IsActive = _engine!.App.PlayState == PlayState.Stop;
            _pauseButton.IsActive = _engine!.App.PlayState == PlayState.Pause;
        }

        public Task StartXr() => Dispatcher.ExecuteAsync(() =>
        {
            try
            {
                _renderSurface.EnableVSync(false);

                _engine!.EnterXr();

                _xrState = SceneXrState.StartRequested;

                _ui.NotifyMessage("XR Session started", MessageType.Info);

            }
            catch (Exception ex)
            {
                _xrButton.IsActive = false;
                _ui.NotifyMessage(ex.Message, MessageType.Error);
            }
            finally
            {
            }
        });

        public Task StopXr() => Dispatcher.ExecuteAsync(() =>
        {
            try
            {
                _renderSurface.EnableVSync(true);
                _engine!.ExitXr();
                _xrState = SceneXrState.StopRequested;
                _ui.NotifyMessage("XR Session stopped", MessageType.Info);
            }
            catch (Exception ex)
            {
                _ui.NotifyMessage(ex.Message, MessageType.Error);
            }
            finally
            {

            }
        });

        protected void RenderLoop()
        {
            _renderSurface.TakeContext();

            CreateAppAsync().Wait();

            _render = _engine!.App.Renderer!;

            _renderSurface.EnableVSync(false);

            while (_isStarted)
            {
                _fpsLabel.Text = _engine!.App.Stats.Fps.ToString();

                if (_scene?.App == null)
                    Thread.Sleep(50);
                else
                {
                    if (_engine.XrApp.IsStarted)
                    {
                        try
                        {
                            _engine.XrApp.RenderFrame(_engine.XrApp.Stage);
                        }
                        catch
                        {

                        }

                        if (_renderSurface.SupportsDualRender)
                        {
                            _render.SetRenderTarget(null);
                            _render.Render(_scene!, _camera!, _view, false);
                        }
                    }
                    else
                        _scene.App!.RenderFrame(_view);

                    _renderSurface.SwapBuffers();

                    OnPropertyChanged(nameof(Stats));
                }

                _renderDispatcher.ProcessQueue();
            }
        }

        protected virtual void OnSceneChanged()
        {
            _main.ExecuteAsync(() =>
            {
                foreach (var tool in _tools)
                    tool.NotifySceneChanged();

                SceneChanged?.Invoke(_scene);
            });
        }

        protected void UpdateSize()
        {
            _view.Width = (uint)(_renderSurface!.Size.X);
            _view.Height = (uint)(_renderSurface.Size.Y);

            if (_camera is PerspectiveCamera persp)
                persp.SetFov(45, _view.Width, _view.Height);
        }

        public Task StartApp() => Dispatcher.ExecuteAsync(() =>
        {
            if (_scene == null || _engine?.App == null || _engine.App.PlayState == PlayState.Start)
                return;

            if (_engine.App.PlayState != PlayState.Pause)
            {
                Log.Info(this, "Saving state...");
                _sceneState = new MemoryStateContainer();
                _scene.GetState(_sceneState);
                Log.Debug(this, "State saved");
            }

            _engine!.App.Start();

            UpdateControls();
        });

        public Task PauseApp() => Dispatcher.ExecuteAsync(() =>
        {
            if (_engine?.App == null)
                return;
            _engine!.App.Pause();
            UpdateControls();
        });

        public Task StopApp() => Dispatcher.ExecuteAsync(() =>
        {
            if (_engine?.App == null || _engine.App.PlayState == PlayState.Stop)
                return;

            _engine!.App.Stop();
            if (_sceneState != null)
            {
                _scene!.SetState(_sceneState);
                _sceneState = null;
            }
            UpdateControls();
        });


        protected void Start()
        {
            if (_isStarted)
                return;

            _isStarted = true;

            _renderSurface!.ReleaseContext();

            _renderThread = new Thread(RenderLoop)
            {
                Name = "XrEngine Render Thread"
            };

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

        public Scene3D? Scene
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
                OnSceneChanged();
                UpdateSize();
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

        public T AddTool<T>(T tool) where T : IEditorTool
        {
            _tools.Add(tool);

            tool.Attach(this);

            return tool;
        }

        public IDispatcher Dispatcher => _renderDispatcher;

        public ToolbarView ToolbarView => _toolbar;

        public event Action<Scene3D?>? SceneChanged;

        public IReadOnlyList<IEditorTool> Tools => _tools;

        public EngineAppStats? Stats => _scene?.App?.Stats;

        public IRenderSurface RenderSurface => _renderSurface;

        public IEnumerable<Camera> CameraList => _scene?.Descendants<Camera>() ?? [];

        public IEditorTool? ActiveTool { get; set; }
    }
}
