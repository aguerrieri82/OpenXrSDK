namespace XrEngine.OpenXr
{
    public interface IRayColliderHandler
    {
        bool UpdateRayView(RayPointerCollider collider, Collision? collision);

        IEnumerable<ICollider3D>? GetColliders();

        bool IsActive { get; }
    }
}
