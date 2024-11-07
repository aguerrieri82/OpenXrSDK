namespace XrEngine
{
    public interface IBehavior : IComponent, IRenderUpdate
    {
        IUpdateGroup? UpdateGroup { get; }
    }
}
