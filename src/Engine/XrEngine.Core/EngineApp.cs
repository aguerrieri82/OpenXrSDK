﻿using XrMath;

namespace XrEngine
{

    public enum PlayState
    {
        Stop,
        Pause,
        Start
    }

    public class EngineApp
    {
        protected readonly HashSet<Scene3D> _scenes = [];
        protected readonly RenderContext _context;
        protected float _startTime;
        protected Scene3D? _activeScene;
        protected readonly EngineAppStats _stats;
        protected PlayState _playState;
        protected readonly QueueDispatcher _dispatcher;
        protected readonly HashSet<IObjectChangeListener> _changeListeners = [];

        public EngineApp()
        {
            _stats = new EngineAppStats();
            _context = new RenderContext();
            //_changeListeners.Add(ShaderMeshLayerBuilder.Instance);
            _dispatcher = new QueueDispatcher();
            //TODO set current by hand (more app in editor)
            if (Current == null)
            {
                Current = this;
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
                _context.StartTime = new TimeSpan(DateTime.Now.Ticks);
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

        public void RenderFrame(Rect2I view, bool flush = true)
        {
            if (_activeScene == null || _activeScene.ActiveCamera == null || Renderer == null)
                return;

            _context.Frame++;
            _context.Scene = _activeScene;

            if (_playState == PlayState.Start)
            {
                var oldTime = _context.Time;

                _context.Time = (new TimeSpan(DateTime.Now.Ticks) - _context.StartTime).TotalSeconds;
                _context.DeltaTime = _context.Time - oldTime;


                _activeScene.Update(_context);
            }

            _stats.BeginFrame();

            try
            {
                _activeScene.DrawGizmos();

                Renderer.Render(_activeScene, _activeScene.ActiveCamera, view, flush);

                _dispatcher.ProcessQueue();
            }
            finally
            {
                _stats.EndFrame();
            }
        }

        protected virtual void OnStarted()
        {

        }

        public IDispatcher Dispatcher => _dispatcher;

        public PlayState PlayState => _playState;

        public ICollection<IObjectChangeListener> ChangeListeners => _changeListeners;

        public IReadOnlyCollection<Scene3D> Scenes => _scenes;

        public EngineAppStats Stats => _stats;

        public Scene3D? ActiveScene => _activeScene;

        public IRenderEngine? Renderer { get; set; }

        public static EngineApp? Current { get; private set; }
    }
}
