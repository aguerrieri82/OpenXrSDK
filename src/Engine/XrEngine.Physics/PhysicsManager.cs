using PhysX.Framework;

namespace XrEngine.Physics
{
    public class PhysicsManager : Behavior<Scene3D>, IDisposable
    {
        protected PhysicsSystem? _system;
        protected Thread? _simulateThread;

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
                _simulateThread.Start();
            }
        }

        public override void Reset(bool onlySelf = false)
        {
            base.Reset(onlySelf);
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
                var delta = _lastUpdateTime - lastStepTime;
                if (delta > 0)
                {
                    _system?.Simulate((float)delta, StepSizeSecs);
                    lastStepTime = _lastUpdateTime;
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

            base.Update(ctx);
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
