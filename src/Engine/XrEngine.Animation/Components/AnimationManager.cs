namespace XrEngine.Animation
{
    public class AnimationManager : Behavior<Scene3D>, IAnimationManager
    {
        protected readonly List<IAnimationControl> _animations = [];


        public IAnimationControl Create(IAnimation animation, IAnimable? host = null)
        {
            var playback = _animations.FirstOrDefault(a =>
                a.Animation == animation &&
                a.Host == host);

            if (playback != null)
                return playback;

            playback = animation.CreateControl(this, host);

            _animations.Add(playback);

            return playback;
        }


        public void Remove(IAnimationControl playback)
        {
            _animations.Remove(playback);
        }


        protected override void Update(RenderContext ctx)
        {
            var referenceTime = (float)Reference.Time;

            for (var i = _animations.Count - 1; i >= 0; i--)
                _animations[i].Step(referenceTime);
        }


        [Action]
        public void StopAll()
        {
            for (var i = _animations.Count - 1; i >= 0; i--)
                _animations[i].Stop();

            _animations.Clear();
        }

        public IReadOnlyCollection<IAnimationControl> ActiveAnimations => _animations;

        public IReferenceTime Reference => _host.Scene!.App!;
    }
}