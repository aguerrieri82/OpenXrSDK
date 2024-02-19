using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Editor
{
    public class SceneView : BasePanel
    {
        protected Camera? _camera;
        protected Scene? _scene;
        protected Thread? _renderThread;
        protected bool _isStarted;
        protected Rect2I _view = new();
        private OpenGLRender _render;
        protected readonly RenderHost _renderHost;

        public SceneView()
        {
            _renderHost = new RenderHost();
            _renderHost.SizeChanged += OnSizeChanged;
            _renderHost.Loaded += OnLoaded;

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
            _render = new OpenGLRender(_renderHost.Gl!);

            if (_scene?.App != null)
                _scene.App.Renderer = _render;

            while (_isStarted)
            {
                if (_scene?.App == null)
                    Thread.Sleep(50);
                else
                {
                    _renderHost!.TakeContext();
                    try
                    {
                        _scene?.App?.RenderFrame(_view);
                        _renderHost.SwapBuffers();
                    }
                    finally
                    {
                        _renderHost.ReleaseContext();
                    }
                }
            }
        }

        protected void UpdateSize()
        {
            _view.Width = (uint)(_renderHost!.RenderSize.Width * 1.25f);
            _view.Height = (uint)(_renderHost.RenderSize.Height * 1.25f);

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
                if (_scene== value)
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

        public RenderHost RenderHost => _renderHost;

        public IEnumerable<Camera> CameraList => _scene?.Descendants<Camera>() ?? [];    
    }
}
