using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public interface IAnimationsHost
    {
        public void AddAnimation(IAnimation animation);

        public void RemoveAnimation(IAnimation animation);

        public void ClearAnimations();


        IReadOnlyList<IAnimation> Animations { get; }
    }
}
