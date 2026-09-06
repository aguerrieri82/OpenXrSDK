namespace XrEngine
{
    public class ComponentLayer<TComp> : BaseAutoLayer<Object3D> where TComp : IComponent
    {

        protected override bool AffectChange(ObjectChange change)
        {
            return change.IsAny(
                ChangeType.Components,
                ChangeType.Visibility,
                ChangeType.Scene);
        }

        protected override bool BelongsToLayer(Object3D obj)
        {
            return obj.IsVisible && obj.Components<TComp>().Any(a => a.IsEnabled);
        }
    }
}
