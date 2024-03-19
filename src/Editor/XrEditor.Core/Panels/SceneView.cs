﻿using OpenXr.Framework.Oculus;
using System.Xml.Linq;
using XrSamples;
using XrEngine;
using XrEngine.Interaction;
using XrEngine.OpenXr;
using XrMath;

namespace XrEditor
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

        public int Id => _pointer.Id;   
    }

    public class SceneView : BasePanel, IStateManager<SceneViewState>
    {
        protected readonly IRenderSurface _renderSurface;

        protected Camera? _camera;
        protected Scene3D? _scene;
        protected Thread? _renderThread;
        protected bool _isStarted;
        protected bool _isXrActive;
        protected Rect2I _view = new();
        protected XrHandInputMesh? _rHand;
        protected XrOculusTouchController? _inputs;
        protected List<IEditorTool> _tools = [];
        protected SceneXrState _xrState;
        protected IRenderEngine? _render;
        protected XrEngineApp? _engine;

        public SceneView(IRenderSurface renderSurface)
        {
            _renderSurface = renderSurface;
            _renderSurface.SizeChanged += OnSizeChanged;
            _renderSurface.Ready += OnSurfaceReady;
        }

        protected void CreateApp()
        {
            _engine = new XrEngineAppBuilder()
              .SetRenderQuality(1, 4) ///samples > 1 cause Filament to fuck up
              .CreatePingPong()
              .Build();

            _engine.App.ActiveScene!.AddComponent(new RayPointerHost(_tools.OfType<PickTool>().Single()));

            Scene = _engine.App.ActiveScene;
        }

        protected void OnSizeChanged(object? sender, EventArgs e)
        {
            UpdateSize();
        }

        protected void OnSurfaceReady(object? sender, EventArgs e)
        {
            AddTool(new PickTool());
            AddTool(new OrbitTool());
            UpdateSize();
            Start();
        }

        public void StartXr()
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
                IsXrActive = false;
                _ui.NotifyMessage(ex.Message, MessageType.Error);
            }
            finally
            {
            }

            OnPropertyChanged(nameof(IsXrActive));
        }

        public void StopXr()
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
        }

        protected void RenderLoop()
        {
            CreateApp();

            _render = _engine!.App.Renderer!;

            _renderSurface.TakeContext();

            while (_isStarted)
            {
                if (_scene?.App == null)
                    Thread.Sleep(50);
                else
                {
                    if (_isXrActive && !_engine.XrApp.IsStarted && _xrState != SceneXrState.StartRequested)
                    {
                        StartXr();
                    }

                    if (!_isXrActive && _engine.XrApp.IsStarted && _xrState != SceneXrState.StopRequested)
                    {
                        StopXr();
                    }

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
                            _render.SetDefaultRenderTarget();
                            _render.Render(_scene!, _camera!, _view, false);
                        }
                    }
                    else
                        _scene.App!.RenderFrame(_view);

                    _renderSurface.SwapBuffers();

                    OnPropertyChanged(nameof(Stats));
                }
            }
        }

        protected virtual void OnSceneChanged()
        {
            foreach (var tool in _tools)
                tool.NotifySceneChanged();
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

            _renderSurface!.ReleaseContext();

            _renderThread = new Thread(RenderLoop)
            {
                Name = "Render Thread"
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

        public bool IsXrActive
        {
            get => _isXrActive;
            set
            {
                if (value == _isXrActive)
                    return;

                _isXrActive = value;

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

        public IEditorTool? ActiveTool { get;  set; }
    }
}
