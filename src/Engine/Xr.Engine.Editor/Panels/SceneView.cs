﻿using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;
using Xr.Engine.Editor.Tools;
using Xr.Engine.OpenXr;

namespace Xr.Engine.Editor
{
    public class SceneView : BasePanel
    {
        protected Camera? _camera;
        protected Scene? _scene;
        protected Thread? _renderThread;
        protected bool _isStarted;
        protected Rect2I _view = new();
        protected readonly RenderHost _renderHost;
        protected XrApp? _xrApp;
        protected XrOculusTouchController? _inputs;
        protected bool _isXrActive;
        protected List<IEditorTool> _tools = [];

        public SceneView()
        {
            _renderHost = new RenderHost();
            _renderHost.SizeChanged += OnSizeChanged;
            _renderHost.Loaded += OnLoaded;
            AddTool(new PickTool());
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
                    new XrGraphicDriver(_renderHost),
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

        public void StartXr()
        {
            if (_xrApp == null)
                CreateXrApp();
            try
            {
                _xrApp!.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IsXrActive = false;
            }

            OnPropertyChanged(nameof(IsXrActive));
        }

        public void StopXr()
        {
            _xrApp?.Stop();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Start();
        }

        private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            UpdateSize();
        }

        protected void RenderLoop()
        {
            var renderOptions = new GlRenderOptions
            {
                FloatPrecision = ShaderPrecision.High,
                ShaderVersion = "300 es",
                RequireTextureCompression = false,   
            };
            var render = new OpenGLRender(_renderHost.Gl!, renderOptions);

            if (_scene?.App != null)
                _scene.App.Renderer = render;

            var windowTarget = new GlDefaultRenderTarget(_renderHost.Gl!);

            _renderHost!.TakeContext();

            while (_isStarted)
            {
                if (_scene?.App == null)
                    Thread.Sleep(50);
                else
                {
                    if (_isXrActive && (_xrApp == null || !_xrApp.IsStarted))
                    {
                        _renderHost.EnableVSync(false);
                        StartXr();
                    }

                    if (!_isXrActive && _xrApp != null && _xrApp.IsStarted)
                    {
                        _renderHost.EnableVSync(true);
                        StopXr();
                    }

                    if (_xrApp != null && _xrApp.IsStarted)
                    {
                        _xrApp.RenderFrame(_xrApp.Stage);
                        render.SetRenderTarget(windowTarget);
                        render.Render(_scene!, _camera!, _view);
                    }
                    else
                        _scene.App!.RenderFrame(_view);

                    _renderHost.SwapBuffers();

                    OnPropertyChanged(nameof(Stats));
                }
            }
        }

        protected void UpdateSize()
        {
            _view.Width = (uint)(_renderHost!.PixelSize.Width);
            _view.Height = (uint)(_renderHost.PixelSize.Height);

            if (_camera is PerspectiveCamera persp)
                persp.SetFov(45, _view.Width, _view.Height);
        }

        protected void Start()
        {
            if (_isStarted)
                return;

            _isStarted = true;

            _renderHost!.ReleaseContext();

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
                OnPropertyChanged(nameof(IsXrActive));
            }
        }

        public T AddTool<T>(T tool) where T : IEditorTool
        {
            _tools.Add(tool);

            tool.Attach(this);

            return tool;
        }

        public IReadOnlyList<IEditorTool> Tools => _tools;

        public EngineAppStats? Stats => _scene?.App?.Stats;

        public RenderHost RenderHost => _renderHost;

        public IEnumerable<Camera> CameraList => _scene?.Descendants<Camera>() ?? [];
    }
}