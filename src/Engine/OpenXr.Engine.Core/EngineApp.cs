using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class EngineApp
    {
        protected IList<Scene> _scenes;
        protected RenderContext _context;
        protected float _startTime;
        protected Scene? _activeScene;
        private bool _isStarted;

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

        public void RenderFrame()
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

            Renderer.Render(_activeScene, _activeScene.ActiveCamera);
        }

        public Scene? ActiveScene => _activeScene;

        public IRenderEngine? Renderer { get; set; }
    }
}
