namespace Xr.Engine.OpenXr
{
    public interface IRayTarget : IComponent
    {
        void NotifyCollision(RenderContext ctx, Collision collision);
    }
}
