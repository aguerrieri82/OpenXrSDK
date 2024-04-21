using PhysX.Framework;

namespace XrEngine.Physics
{
    public class PhysicsManager : Behavior<Scene3D>, IDisposable
    {
        protected PhysicsSystem? _system;

        public PhysicsManager()
        {
            Options = new PhysicsOptions();
            StepSizeSecs = 1f / 40f;
        }

        protected override void Start(RenderContext ctx)
        {
            Destroy();
            _system = new PhysicsSystem();
            _system.Create(Options);
            _system.CreateScene(Options.Gravity);
        }

        protected void Destroy()
        {
            if (_system != null)
            {
                _system.Dispose();
                _system = null;
            }
        }

        protected override void Update(RenderContext ctx)
        {
            _system?.Simulate((float)DeltaTime, StepSizeSecs);

            base.Update(ctx);
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        public float StepSizeSecs { get; set; }

        public PhysicsOptions Options { get; set; }

        public PhysicsSystem System => _system ?? throw new NullReferenceException();
    }
}
