using PhysX.Framework;
using System.Collections.Concurrent;
using System.Diagnostics;
using XrMath;

namespace XrEngine.Physics
{
    public class PhysicsManager : Behavior<Scene3D>, IDisposable
    {
        protected PhysicsSystem? _system;
        protected Thread? _simulateThread;
        protected ConcurrentQueue<Action> _queue = [];
        protected HashSet<Joint> _joints = [];
        protected static Stopwatch _stopwatch = Stopwatch.StartNew();

        public PhysicsManager()
        {
            Options = new PhysicsOptions();
            StepSizeSecs = 1f / 70f;
            IsMultiThread = false;
        }

        protected override void Start(RenderContext ctx)
        {
            Destroy();

            _system = new PhysicsSystem();
            _system.Create(Options);
            _system.CreateScene(Options.Gravity);

            Configure?.Invoke(_system);

            foreach (var joint in _joints)
                joint.Create(ctx);

            if (IsMultiThread)
            {
                _simulateThread = new Thread(SimulateLoopV2);
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
            foreach (var joint in _joints)
                joint.Destroy();

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

        void SimulateLoopV2()
        {
            while (IsStarted)
            {
                var curTime = _stopwatch.Elapsed;

                while (_queue.TryDequeue(out var action))
                    action();

                _system?.Simulate((float)StepSizeSecs, StepSizeSecs);

                var ellapsed = (_stopwatch.Elapsed - curTime).TotalSeconds;

                var wait = StepSizeSecs - ellapsed; 

                if (wait > 0)
                    Thread.Sleep(TimeSpan.FromSeconds(wait));
                else
                    Thread.Yield();
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

        public void RemoveJoint(Joint joint)
        {
            joint.Dispose();
            _joints.Remove(joint);
        }

        public Joint AddJoint(JointType type, Object3D object0, Pose3 pose0, Object3D object1, Pose3 pose1)
        {
            var joint = new Joint
            {
                Type = type,
                Object0 = object0,
                Pose0 = pose0,
                Object1 = object1,
                Pose1 = pose1
            };

            _joints.Add(joint);

            object0.AddComponent(new JointConnection(joint, 0));
            object1.AddComponent(new JointConnection(joint, 1));

            return joint;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(StepSizeSecs), StepSizeSecs);
            container.Write(nameof(IsMultiThread), IsMultiThread);
            container.Write(nameof(Options), Options); 
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            IsMultiThread = container.Read<bool>(nameof(IsMultiThread));
            StepSizeSecs = container.Read<float>(nameof(StepSizeSecs));
            Options = container.Read<PhysicsOptions>(nameof(Options));
        }


        public Action<PhysicsSystem>? Configure { get; set; }

        public float StepSizeSecs { get; set; }

        public PhysicsOptions Options { get; set; }

        public bool IsMultiThread { get; set; }

        public PhysicsSystem? System => _system;

        public IReadOnlyCollection<Joint> Joint => _joints;
    }
}
