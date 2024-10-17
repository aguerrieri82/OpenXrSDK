using PhysX.Framework;
using System.Collections.Concurrent;

namespace XrEngine.Physics
{
    public class PhysicsManager : Behavior<Scene3D>, IDisposable
    {
        protected PhysicsSystem? _system;
        protected Thread? _simulateThread;
        protected ConcurrentQueue<Action> _queue = [];

        public PhysicsManager()
        {
            Options = new PhysicsOptions();
            StepSizeSecs = 1f / 40f;
            IsMultiThread = false;
        }

        protected override void Start(RenderContext ctx)
        {
            Destroy();
            _system = new PhysicsSystem();
            _system.Create(Options);
            _system.CreateScene(Options.Gravity);

            if (IsMultiThread)
            {
                _simulateThread = new Thread(SimulateLoop);
                _simulateThread.Name = "XrEngine PhysicsSimulate";
                _simulateThread.Start();
            }
        }

        public override void Reset(bool onlySelf = false)
        {
            _queue.Clear();
            base.Reset(onlySelf);
        }

        public void Execute(Action action)
        {
            if (!_isEnabled)
                return;

            if (IsMultiThread)
                _queue.Enqueue(action);
            else
                action();
        }

        protected void Destroy()
        {
            if (_system != null)
            {
                _system.Dispose();
                _system = null;
            }
        }

        void SimulateLoop()
        {
            var lastStepTime = _lastUpdateTime;

            while (IsStarted)
            {
                var curTime = _lastUpdateTime;

                var delta = curTime - lastStepTime;

                if (delta > 0)
                {
                    while (_queue.TryDequeue(out var action))
                        action();

                    _system?.Simulate((float)delta, StepSizeSecs);

                    lastStepTime = curTime;
                }
                else
                    Thread.Sleep(1);
            }
        }

        protected override void Update(RenderContext ctx)
        {
            if (!IsMultiThread)
                _system?.Simulate((float)DeltaTime, StepSizeSecs);
            else
                _lastUpdateTime = ctx.Time;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        public float StepSizeSecs { get; set; }

        public PhysicsOptions Options { get; set; }

        public bool IsMultiThread { get; set; }

        public PhysicsSystem? System => _system;
    }
}
