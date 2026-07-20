using PhysX.Framework;
using System.Collections.Concurrent;
using System.Diagnostics;
using XrMath;

namespace XrEngine.Physics
{
    [Flags]
    public enum PhysicsDebugGizmos
    {
        None = 0,
        Joints = 0x1
    }

    public class PhysicsManager : Behavior<Scene3D>, IDisposable, IReferenceTime
    {
        protected PhysicsSystem? _system;
        protected Thread? _simulateThread;
        protected ConcurrentQueue<Action> _queue = [];
        protected HashSet<Joint> _joints = [];
        protected HashSet<Joint> _jointToCreate = [];
        protected readonly List<CollideGroup> _collideGroups = [];

        public PhysicsManager(float fps = 72)
        {
            Options = new PhysicsOptions();
            StepSizeSecs = fps == 0 ? 0 : 1f / fps;
            UpdatePriority = -1;
            UseQueue = true;
            IsMultiThread = false;
        }

        protected override void Start(RenderContext ctx)
        {
            Destroy();

            _system = new PhysicsSystem();
            _system.Create(Options);
            _system.CreateScene(Options.Gravity);
            _system.CollideGroups = _collideGroups;

            Configure?.Invoke(_system);

            foreach (var joint in _joints)
                joint.Create(ctx);

            if (IsMultiThread)
            {
                _simulateThread = new Thread(SimulateLoop)
                {
                    Name = "XrEngine PhysicsSimulate"
                };

                _simulateThread.Start();
            }
        }

        public void SetCollideGroup(RigidBodyGroup group, CollideGroup grp)
        {
            var index = (int)MathF.Log2((int)group);

            while (index >= _collideGroups.Count)
                _collideGroups.Add(CollideGroup.Always);

            _collideGroups[index] = grp;
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

            if (IsMultiThread && UseQueue)
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
            var realStart = Stopwatch.GetTimestamp();

            double nextUpdateTime = StepSizeSecs;

            using var sync = new AutoResetEvent(false);

            while (IsStarted && _system != null)
            {
                if (!_queue.IsEmpty)
                {
                    EngineApp.Current.Dispatcher.Post(() =>
                    {
                        while (_queue.TryDequeue(out var action))
                            action();

                        sync.Set();
                    });

                    sync.WaitOne();
                }

                var realTime = Stopwatch.GetElapsedTime(realStart).TotalSeconds;
                var deltaSecs = realTime - _system.Time;

                if (deltaSecs > 0)
                    _system.Simulate((float)deltaSecs, (float)StepSizeSecs);

                realTime = Stopwatch.GetElapsedTime(realStart).TotalSeconds;
                var waitSecs = nextUpdateTime - realTime;

                if (waitSecs > 0)
                    EngineNativeLib.SleepFor((ulong)(waitSecs * 1e9));

                nextUpdateTime += StepSizeSecs;

                if (nextUpdateTime < realTime)
                    nextUpdateTime = realTime + StepSizeSecs;
            }
        }

        protected override void Update(RenderContext ctx)
        {
            //Debug.WriteLine("{0} Simulate Start", Time);

            if (_jointToCreate.Count > 0)
            {
                var lockWrite = _system?.Scene.LockWrite();

                foreach (var joint in _jointToCreate)
                    joint.Create(ctx);

                _jointToCreate.Clear();
            }

            if (ctx.Time < 0.5)
                return;

            if (!IsMultiThread)
                _system?.Simulate((float)DeltaTime, StepSizeSecs > 0 ? StepSizeSecs : (float)DeltaTime);
            else
                _lastUpdateTime = ctx.Time;

            //Debug.WriteLine("{0} Simulate End", Time);
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

            if (!pose0.IsFinite() || !pose1.IsFinite())
                throw new Exception();

            _joints.Add(joint);

            object0.AddComponent(new JointConnection(joint, 0));
            object1.AddComponent(new JointConnection(joint, 1));

            if (IsStarted)
                _jointToCreate.Add(joint);

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

        public bool UseQueue { get; set; }

        public PhysicsDebugGizmos DebugGizmos { get; set; }

        public Action<PhysicsSystem>? Configure { get; set; }

        public float StepSizeSecs { get; set; }

        public PhysicsOptions Options { get; set; }

        public bool IsMultiThread { get; set; }

        public double Time => _system?.Time ?? 0;

        public PhysicsSystem? System => _system;

        public IReadOnlyCollection<Joint> Joint => _joints;

    }
}
