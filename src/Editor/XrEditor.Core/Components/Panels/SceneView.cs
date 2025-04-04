
using OpenXr.Framework.Oculus;
using System.ComponentModel;
using System.Numerics;
using System.Xml.Linq;
using XrEditor.Services;
using XrEngine;
using XrEngine.OpenXr;
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


    [Panel("e0c28154-c76f-4765-83dc-0fe9ceb6f655")]
    [DisplayName("Scene")]
    public class SceneView : BasePanel
    {
        protected readonly IRenderSurface _renderSurface;


        protected Camera _camera;
        protected Scene3D? _scene;
        protected Thread? _renderThread;
        protected bool _isStarted;
        protected XrHandInputMesh? _rHand;
        protected XrOculusTouchController? _inputs;
        protected SceneXrState _xrState;
        protected IRenderEngine? _render;
        protected XrEngineApp? _engine;
        protected MemoryStateContainer? _sceneState;
        protected SingleSelector _cameraList;
        protected readonly QueueDispatcher _renderDispatcher;
        protected readonly ActionView _playButton;
        protected readonly ActionView _pauseButton;
        protected readonly ActionView _stopButton;
        protected readonly ActionView _xrButton;
        protected readonly TextView _fpsLabel;
        protected readonly PerspectiveCamera _sceneCamera;
        protected readonly List<IEditorTool> _tools = [];

        public SceneView(IRenderSurface renderSurface)
        {
            _renderSurface = renderSurface;
            _renderSurface.SizeChanged += OnSizeChanged;
            _renderSurface.Ready += OnSurfaceReady;
            ToolBar = new ToolbarView();
            _renderDispatcher = new QueueDispatcher();
            _xrButton = ToolBar.AddToggle("icon_visibility", false, value =>
            {
                if (value)
                    StartXr();
                else
                    StopXr();
            });

            _sceneCamera = new PerspectiveCamera
            {
                Far = 100f,
                Near = 0.01f,
                BackgroundColor = new Color(0, 0, 0, 0),
                Exposure = 1,
                FovDegree = 45,
                Name = "Scene"
            };

            _sceneCamera.LookAt(new Vector3(1, 1.7f, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            _camera = _sceneCamera;

            _fpsLabel = ToolBar.AddText(string.Empty);
            ToolBar.AddDivider();
            _playButton = ToolBar.AddButton("icon_play_arrow", StartApp);
            _pauseButton = ToolBar.AddButton("icon_pause", PauseApp);
            _stopButton = ToolBar.AddButton("icon_stop", StopApp);
            ToolBar.AddDivider();
            _cameraList = ToolBar.AddSelect(ListCameras(), _camera, c => Camera = c);
            _cameraList.ValueType = typeof(Camera);

        }

        protected IList<SelectorItem> ListCameras()
        {
            var result = new List<SelectorItem>();
            result.Add(new SelectorItem { DisplayName = "Scene", Value = _sceneCamera });
            if (_scene != null)
            {
                foreach (var camera in _scene.Descendants<PerspectiveCamera>())
                    result.Add(new SelectorItem
                    {
                        Value = camera,
                        DisplayName = camera.Name ?? $"Camera {result.Count}"
                    });
            }


            return result;
        }

        protected async Task CreateAppAsync()
        {
            _engine = EditorDebug.CreateApp();

            _engine.App.ActiveScene!.AddComponent(new RayPointerHost(_tools.OfType<PickTool>().Single()));

            await _mainDispatcher.ExecuteAsync(() =>
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

        public Task StartXr() => RenderDispatcher.ExecuteAsync(() =>
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

        public Task StopXr() => RenderDispatcher.ExecuteAsync(() =>
        {
            try
            {
                if (!_engine!.XrApp.IsStarted)
                    return;

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

            _renderSurface.EnableVSync(EditorDebug.EnableVSync);

            while (_isStarted)
            {
                _fpsLabel.Text = _engine!.App.Stats.Fps.ToString();

                if (_scene?.App == null || !_isActive)
                    Thread.Sleep(50);
                else
                {
                    if (_engine.XrApp.IsStarted)
                    {
                        try
                        {
                            _engine.XrApp.RenderFrame(_engine.XrApp.ReferenceSpace);
                        }
                        catch
                        {
                        }

                        if (_renderSurface.SupportsDualRender)
                        {
                            _camera.IsStereo = false;
                            _render.SetRenderTarget(null);
                            _scene.App.RenderScene(_camera);
                        }
                    }
                    else
                        _scene.App.RenderFrame(_camera);

                    _renderSurface.SwapBuffers();

                    OnPropertyChanged(nameof(Stats));
                }

                _renderDispatcher.ProcessQueue();
            }
        }

        protected virtual void OnSceneChanged()
        {
            _ = _mainDispatcher.ExecuteAsync(() =>
            {
                foreach (var tool in _tools)
                    tool.NotifySceneChanged();

                SceneChanged?.Invoke(_scene);
            });
        }

        protected void UpdateSize()
        {
            var width = (uint)(_renderSurface!.Size.X);
            var height = (uint)(_renderSurface.Size.Y);

            Log.Info(this, "New render size: {0}x{1}", width, height);

            width = (uint)Math.Ceiling(width / 2.0f) * 2;
            height = (uint)Math.Ceiling(height / 2.0f) * 2;

            if (_camera is PerspectiveCamera persp)
            {
                if (persp.FovDegree == 0)
                    persp.FovDegree = 45;
                persp.SetFov(persp.FovDegree, width, height);
            }
        }

        public Task StartApp() => RenderDispatcher.ExecuteAsync(() =>
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

        public Task PauseApp() => RenderDispatcher.ExecuteAsync(() =>
        {
            if (_engine?.App == null)
                return;
            _engine!.App.Pause();
            UpdateControls();
        });

        public Task StopApp() => RenderDispatcher.ExecuteAsync(() =>
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
            StopApp();

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
                _cameraList.Items = ListCameras();

                OnPropertyChanged(nameof(Scene));

                OnSceneChanged();
                UpdateSize();
            }
        }

        public Camera Camera
        {
            get => _camera;
            set
            {
                if (_camera == value)
                    return;
                _camera = value;
                UpdateSize();
                OnPropertyChanged(nameof(Camera));
            }
        }

        public T AddTool<T>(T tool) where T : IEditorTool
        {
            _tools.Add(tool);

            tool.Attach(this);

            return tool;
        }

        public IDispatcher RenderDispatcher => _renderDispatcher;

        public IReadOnlyList<IEditorTool> Tools => _tools;

        public EngineAppStats? Stats => _scene?.App?.Stats;

        public IRenderSurface RenderSurface => _renderSurface;

        public IEditorTool? ActiveTool { get; set; }


        public event Action<Scene3D?>? SceneChanged;

        public override string? Title => "Scene";
    }
}
