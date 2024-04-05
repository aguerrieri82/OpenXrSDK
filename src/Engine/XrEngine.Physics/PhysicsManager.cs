using PhysX.Framework;

namespace XrEngine.Physics
{
    public class PhysicsManager : Behavior<Scene3D>
    {
        readonly PhysicsSystem _system;

        public PhysicsManager()
        {
            _system = new PhysicsSystem();
            Options = new PhysicsOptions();
            StepSizeSecs = 1f / 40f;
        }

        protected override void Start(RenderContext ctx)
        {
            _system.Create(Options);
            _system.CreateScene(Options.Gravity);
        }

        protected override void Update(RenderContext ctx)
        {
            _system.Simulate((float)DeltaTime, StepSizeSecs);

            base.Update(ctx);
        }

        public float StepSizeSecs { get; set; }

        public PhysicsOptions Options { get; set; }

        public PhysicsSystem System => _system;
    }
}
