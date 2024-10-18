namespace XrEngine.Interaction
{
    public interface IRayTarget : IComponent
    {
        void NotifyCollision(RenderContext ctx, Collision? collision);
    }
}
