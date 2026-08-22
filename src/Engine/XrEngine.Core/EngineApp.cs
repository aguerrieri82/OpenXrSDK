
using System.Diagnostics;
using XrMath;

namespace XrEngine
{

    public enum PlayState
    {
        Stop,
        Pause,
        Start
    }

    public class EngineApp : IAsyncDisposable, IReferenceTime
    {
        static EngineApp? _current;

        protected readonly HashSet<Scene3D> _scenes = [];
        protected readonly RenderContext _context;
        protected float _startTime;
        protected Scene3D? _activeScene;
        protected readonly EngineAppStats _stats;
        protected PlayState _playState;
        protected IRenderEngine? _renderer;
        protected int _captureCount;
        protected readonly QueueDispatcher _dispatcher;
        protected readonly HashSet<IObjectChangeListener> _changeListeners = [];

        public EngineApp()
        {
            _stats = new EngineAppStats();
            _context = new RenderContext();
            _dispatcher = new QueueDispatcher();

            //TODO set current by hand (more app in editor)
            if (_current == null)
            {
                _current = this;
                Context.Implement(this);
            }
        }

        public void AddScene(Scene3D scene)
        {
            _scenes.Add(scene);
            scene.Attach(this);
        }

        public void OpenScene(Scene3D scene)
        {
            if (_activeScene == scene)
                return;

            if (!_scenes.Contains(scene))
                AddScene(scene);

            _activeScene = scene;

            Scene3D.Current = scene;
        }

        public void Start()
        {
            if (_playState == PlayState.Start)
                return;

            if (_playState == PlayState.Stop)
            {
                _context.StartTime = new TimeSpan(DateTime.UtcNow.Ticks);
                _context.Frame = 0;
            }

            _playState = PlayState.Start;

            OnStarted();
        }

        public void Pause()
        {
            if (_playState != PlayState.Start)
                return;

            _playState = PlayState.Pause;
        }

        public void Stop()
        {
            if (_playState == PlayState.Stop)
                return;

            _playState = PlayState.Stop;
            _activeScene?.Reset();
        }

        public bool BeginFrame()
        {
            _dispatcher.ProcessQueue();

            if (_activeScene == null || _activeScene.ActiveCamera == null || _renderer == null)
                return false;

            if (_captureCount > 0)
                EngineNativeLib.RdcStartFrameCapture();

            _context.Frame++;
            _context.Scene = _activeScene;

            if (_playState == PlayState.Start)
            {
                var oldTime = _context.Time;

                if (ReferenceTime != null)
                    _context.Time = ReferenceTime.Time;
                else
                    _context.Time = (new TimeSpan(DateTime.UtcNow.Ticks) - _context.StartTime).TotalSeconds;

                _context.DeltaTime = _context.Time - oldTime;

                _activeScene.Update(_context);
            }

            _activeScene.DrawGizmos(_context);

            _stats.BeginFrame();

            return true;
        }

        public void RenderScene(Camera? camera = null, bool flush = true)
        {
            if (_activeScene == null || Renderer == null)
                return;

            _context.Camera = camera ?? _activeScene.ActiveCamera;

            if (_context.Camera == null)
                return;

            if (_context.Camera.Scene != _activeScene)
                _context.Camera._scene = _activeScene;

            Renderer.Render(_context, new Rect2I(_context.Camera.ViewSize), flush);
        }

        public void EndFrame()
        {
            if (_captureCount > 0)
            {
                EngineNativeLib.RdcEndFrameCapture(true);
                _captureCount--;
            }

            _stats.EndFrame();
        }

        public void RenderFrame(Camera? camera = null, bool flush = true)
        {
            if (!BeginFrame())
                return;
            try
            {
                RenderScene(camera, flush);
            }
            finally
            {
                EndFrame();
            }
        }

        protected virtual void OnStarted()
        {

        }

        public virtual ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        public void CaptureFrames(int count)
        {
            _captureCount = count;
        }


        [Conditional("DEBUG")]
        public static void VerifyMainThread(object caller)
        {
            if (caller is Object3D obj3d && obj3d.Scene == null)
                return;
            if (caller is Geometry3D geo3d && !geo3d.Hosts.OfType<Object3D>().Any(a => a.Scene != null))
                return;
            Debug.Assert(_current?.Dispatcher.Thread == Thread.CurrentThread);
        }

        public RenderContext RenderContext => _context;

        public QueueDispatcher Dispatcher => _dispatcher;

        public PlayState PlayState => _playState;

        public ICollection<IObjectChangeListener> ChangeListeners => _changeListeners;

        public IReadOnlyCollection<Scene3D> Scenes => _scenes;

        public EngineAppStats Stats => _stats;

        public Scene3D? ActiveScene => _activeScene;

        public bool HasRenderer => _renderer != null;

        public IRenderEngine Renderer
        {
            get => _renderer ?? throw new NotSupportedException();

            set => _renderer = value;
        }

        public IReferenceTime? ReferenceTime { get; set; }

        public static EngineApp Current
        {
            set => _current = value;
            get => _current ?? throw new NotSupportedException();
        }

        public static bool IsCreated => _current != null;

        public static DispatcherSwitch MainThread => Current.Dispatcher.Switch;

        public static DispatcherSwitch RenderThread => Current.Renderer.Dispatcher.Switch;

        double IReferenceTime.Time => _context.Time;
    }
}
