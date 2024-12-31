namespace XrEngine
{
    public class LightLayer : BaseAutoLayer<Light>
    {
        public LightLayer()
        {
            Name = "Light";
        }

        protected override bool AffectChange(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Scene, ObjectChangeType.Render, ObjectChangeType.Visibility))
                return true;

            return false;
        }

        protected override bool BelongsToLayer(Light obj)
        {
            return obj.IsVisible;
        }
    }

}
