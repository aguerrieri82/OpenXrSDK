namespace XrEngine.Animation
{
    public interface IAnimationManager
    {
        IAnimationControl Create(IAnimation animation, IAnimable? host = null);

        void Remove(IAnimationControl playback);

        IReadOnlyCollection<IAnimationControl> ActiveAnimations { get; }

        IReferenceTime Reference { get; }
    }
}