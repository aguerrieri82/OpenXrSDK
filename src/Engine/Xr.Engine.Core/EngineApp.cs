using Xr.Math;

namespace Xr.Engine
{
    public class EngineAppStats
    {
        protected int _fpsFrameCount;
        protected DateTime _fpsLastTime;

        public void BeginFrame()
        {

        }

        public void EndFrame()
        {
            _fpsFrameCount++;

            var deltaSecs = (DateTime.Now - _fpsLastTime).TotalSeconds;

            if (deltaSecs >= 2)
            {
                Fps = (int)(_fpsFrameCount / deltaSecs);
                _fpsFrameCount = 0;
                _fpsLastTime = DateTime.Now;
            }
        }

        public int Fps { get; protected set; }
    }

    public class EngineApp
    {
        protected readonly HashSet<Scene> _scenes = [];
        protected RenderContext _context;
        protected float _startTime;
        protected Scene? _activeScene;
        protected EngineAppStats _stats;
        protected bool _isStarted;
        protected readonly HashSet<IObjectChangeListener> _changeListeners = [];


        public EngineApp()
        {
            _stats = new EngineAppStats();
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

        public void RenderFrame(Rect2I view, bool flush = true)
        {
            if (_activeScene == null || _activeScene.ActiveCamera == null || Renderer == null)
                return;

            _context.Frame++;

            var oldTime = _context.Time;

            _context.Time = (new TimeSpan(DateTime.Now.Ticks) - _context.StartTime).TotalSeconds;
            _context.DeltaTime = _context.Time - oldTime;

            _activeScene.Update(_context);

            _stats.BeginFrame();

            try
            {
                Renderer.Render(_activeScene, _activeScene.ActiveCamera, view, flush);
            }
            finally
            {
                _stats.EndFrame();
            }
        }

        public ICollection<IObjectChangeListener> ChangeListeners => _changeListeners;

        public IReadOnlyCollection<Scene> Scenes => _scenes;

        public EngineAppStats Stats => _stats;

        public Scene? ActiveScene => _activeScene;

        public IRenderEngine? Renderer { get; set; }

        public static EngineApp? Current { get; private set; }
    }
}
