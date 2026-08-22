namespace XrEngine.Animation
{
    public interface IComputedAnimation : IAnimation
    {
        IComputeFunction Compute { get; }
    }
}
