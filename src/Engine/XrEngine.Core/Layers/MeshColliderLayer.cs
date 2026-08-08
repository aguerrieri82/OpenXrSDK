namespace XrEngine
{
    public class MeshColliderLayer : ComponentLayer<MeshCollider>
    {
        protected override bool BelongsToLayer(Object3D obj)
        {
            return obj.IsVisible &&
                obj.Components<MeshCollider>().Any(a =>
                        a.IsEnabled &&
                        (a.Usage & ColliderUsage.Collisions) != 0);
        }
    }
}
