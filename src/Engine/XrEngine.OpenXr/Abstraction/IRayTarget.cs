namespace XrEngine.OpenXr
{
    public interface IRayTarget : IComponent
    {
        void NotifyCollision(RenderContext ctx, Collision collision);
    }
}
