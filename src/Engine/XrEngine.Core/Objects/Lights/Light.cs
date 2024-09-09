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
            if (change.IsAny(ObjectChangeType.Property, ObjectChangeType.Transform))
                change.Type |= ObjectChangeType.Render;

            if (change.IsAny(ObjectChangeType.Render))
                Version++;
           
            base.OnChanged(change);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<Light>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<Light>(this);
        }

        public bool CastShadows { get; set; }

        public Color Color { get; set; }

        [Range(0, 10, 0.1f)]
        public float Intensity { get; set; }
    }
}
