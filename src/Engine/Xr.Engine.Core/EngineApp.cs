namespace OpenXr.Engine
{
    public class EngineApp
    {
        protected readonly HashSet<Scene> _scenes = [];
        protected RenderContext _context;
        protected float _startTime;
        protected Scene? _activeScene;
        protected int _fpsFrameCount;
        protected DateTime _fpsLastTime;
        protected bool _isStarted;
        protected readonly HashSet<IObjectChangeListener> _changeListeners = [];


        public EngineApp()
        {
     
            _context = new RenderContext();
            _changeListeners.Add(ShaderMeshLayerBuilder.Instance);
            Current = this;
        }

        public void AddScene(Scene scene)
        {
            _scenes.Add(scene);
            scene.Attach(this);
        }

        public void OpenScene(Scene scene)
        {
            if (_activeScene == scene)
                return;

            if (!_scenes.Contains(scene))
                AddScene(scene);

            _activeScene = scene;

            Scene.Current = scene;
        }

        public void Start()
        {
            if (_isStarted)
                return;
            _context.StartTime = new TimeSpan(DateTime.Now.Ticks);
            _context.Frame = 0;
        }

        public void Stop()
        {
            if (!_isStarted)
                return;
        }

        protected void UpdateFps()
        {
            _fpsFrameCount++;
            var deltaSecs = (DateTime.Now - _fpsLastTime).TotalSeconds;
            if (deltaSecs >= 2)
            {
                _context.Fps = (int)(_fpsFrameCount / deltaSecs);
                _fpsFrameCount = 0;
                _fpsLastTime = DateTime.Now;
                Console.WriteLine($"Fps: {_context.Fps}");
            }
        }

        public void RenderFrame(Rect2I view)
        {
            _context.Frame++;
            _context.Time = (new TimeSpan(DateTime.Now.Ticks) - _context.StartTime).TotalSeconds;

            if (_activeScene == null)
                return;

            _activeScene.Update(_context);

            if (_activeScene.ActiveCamera == null)
                return;

            if (Renderer == null)
                return;

            //Console.WriteLine($"Render frame {_context.Frame}");

            Renderer.Render(_activeScene, _activeScene.ActiveCamera, view);

            UpdateFps();
        }

        public ICollection<IObjectChangeListener> ChangeListeners => _changeListeners;

        public IReadOnlyCollection<Scene> Scenes => _scenes;

        public Scene? ActiveScene => _activeScene;

        public IRenderEngine? Renderer { get; set; }

        public static EngineApp? Current { get; private set; }
    }
}
