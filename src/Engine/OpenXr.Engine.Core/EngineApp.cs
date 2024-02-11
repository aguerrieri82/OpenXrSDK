namespace OpenXr.Engine
{
    public class EngineApp
    {
        protected IList<Scene> _scenes;
        protected RenderContext _context;
        protected float _startTime;
        protected Scene? _activeScene;
        private int _fpsFrameCount;
        private DateTime _fpsLastTime;
        private readonly bool _isStarted;

        public EngineApp()
        {
            _scenes = new List<Scene>();
            _context = new RenderContext();
        }

        public void AddScene(Scene scene)
        {
            _scenes.Add(scene);
        }

        public void OpenScene(string name)
        {
            OpenScene(_scenes.Single(s => s.Name == name));
        }

        public void OpenScene(Scene scene)
        {
            if (!_scenes.Contains(scene))
                AddScene(scene);
            _activeScene = scene;
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

        public void RenderFrame(RectI view)
        {

            _context.Frame++;
            _context.Time = (new TimeSpan(DateTime.Now.Ticks) - _context.StartTime).TotalSeconds;

            if (_activeScene == null)
                return;

            _activeScene.Update(_context);
            _activeScene.UpdateWorldMatrix(true, false);

            if (_activeScene.ActiveCamera == null)
                return;

            if (Renderer == null)
                return;

            //Console.WriteLine($"Render frame {_context.Frame}");

  
            Renderer.Render(_activeScene, _activeScene.ActiveCamera, view);

            UpdateFps();
        }

        public Scene? ActiveScene => _activeScene;

        public IRenderEngine? Renderer { get; set; }
    }
}
