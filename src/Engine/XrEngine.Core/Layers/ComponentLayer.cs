namespace XrEngine
{
    public class ComponentLayer<TComp> : BaseAutoLayer<Object3D> where TComp : IComponent
    {

        protected override bool AffectChange(ObjectChange change)
        {
            return change.IsAny(
                ObjectChangeType.Components,
                ObjectChangeType.Visibility,
                ObjectChangeType.Scene);
        }

        protected override bool BelongsToLayer(Object3D obj)
        {
            return obj.IsVisible && obj.Components<TComp>().Any();
        }
    }
}
