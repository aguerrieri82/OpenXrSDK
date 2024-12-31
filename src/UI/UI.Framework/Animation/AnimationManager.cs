namespace CanvasUI
{
    public class AnimationManager
    {
        readonly Thread _thread;
        readonly HashSet<IAnimation> _animations;
        bool _isStarted;

        AnimationManager()
        {
            _isStarted = true;
            _thread = new Thread(Update);
            _thread.Name = "UI Animation";
            _thread.Start();
            _animations = [];
            FrameRate = 60;
        }

        public void Start(IAnimation animation)
        {
            lock (_animations)
            {
                animation.IsStarted = false;
                _animations.Add(animation);
                Monitor.Pulse(_animations);
            }
        }

        public void Stop()
        {
            if (!_isStarted)
                return;

            _isStarted = false;
            lock (_animations)
                Monitor.Pulse(_animations);
            _thread.Join();
        }

        protected void Update()
        {
            var startTime = DateTime.UtcNow;

            var toRemove = new HashSet<IAnimation>();

            while (_isStarted)
            {
                lock (_animations)
                {
                    while (_animations.Count == 0 && _isStarted)
                        Monitor.Wait(_animations);
                }

                if (!_isStarted)
                    return;

                var curTime = DateTime.UtcNow - startTime;

                toRemove.Clear();

                IList<IAnimation> animations;

                lock (_animations)
                    animations = _animations.ToArray();

                foreach (var animation in animations)
                {
                    if (!animation.IsStarted)
                    {
                        animation.StartTime = curTime;
                        animation.IsStarted = true;
                    }

                    var t = (float)((curTime - animation.StartTime).TotalMilliseconds / animation.Duration.TotalMilliseconds);

                    if (t > 1)
                        t = 1;

                    animation.Step(t);

                    if (t == 1)
                    {
                        animation.IsStarted = false;
                        toRemove.Add(animation);
                    }
                }

                lock (_animations)
                {
                    foreach (var item in toRemove)
                        _animations.Remove(item);
                }

                if (_animations.Count > 0)
                    Thread.Sleep(TimeSpan.FromSeconds(1f / FrameRate));
            }
        }


        public int FrameRate { get; set; }


        public static readonly AnimationManager Instance = new AnimationManager();
    }
}
