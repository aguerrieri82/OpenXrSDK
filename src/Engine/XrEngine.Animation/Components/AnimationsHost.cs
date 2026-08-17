using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public class AnimationsHost : BaseComponent<Object3D>, IAnimationsHost
    {
        private readonly List<IAnimation> _animations = [];

        public void AddAnimation(IAnimation animation)
        {
            if (!_animations.Contains(animation))
                _animations.Add(animation);
        }

        public void RemoveAnimation(IAnimation animation)
        {
            _animations.Remove(animation);
        }

        public void ClearAnimations()
        {
            _animations.Clear();
        }


        [Action]
        public void Animate()
        {
            _host.Animate("Take 001");
        }

        public IReadOnlyList<IAnimation> Animations => _animations;
    }
}
