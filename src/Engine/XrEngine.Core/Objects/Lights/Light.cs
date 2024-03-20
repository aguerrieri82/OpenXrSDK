using XrMath;

namespace XrEngine
{
    public abstract class Light : Object3D
    {
        public Light()
        {
            Color = Color.White;
            Intensity = 1f;
        }

        public override void NotifyChanged(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Render))
                Version++;

            base.NotifyChanged(change);
        }

        public Color Color { get; set; }

        public float Intensity { get; set; }
    }
}
