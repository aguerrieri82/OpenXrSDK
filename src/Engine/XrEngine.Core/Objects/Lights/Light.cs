using XrMath;

namespace XrEngine
{
    public abstract class Light : Object3D
    {
        public Light()
        {
            Color = Color.White;
            Intensity = 1f;
            CastShadows = true; 
        }

        protected override void OnChanged(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Render))
                Version++;
            base.OnChanged(change);
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.WriteObject<Light>(this);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            container.ReadObject<Light>(this);
        }

        public bool CastShadows { get; set; }

        public Color Color { get; set; }

        public float Intensity { get; set; }
    }
}
