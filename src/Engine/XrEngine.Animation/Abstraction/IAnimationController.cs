namespace XrEngine.Animation
{
    public interface IAnimationController
    {
        IAnimationPlayback CreatePlayback(IAnimation animation, IAnimable? host = null);

        void Remove(IAnimationPlayback playback);

        IReadOnlyCollection<IAnimationPlayback> ActiveAnimations { get; }

        IReferenceTime Reference { get; }
    }
}