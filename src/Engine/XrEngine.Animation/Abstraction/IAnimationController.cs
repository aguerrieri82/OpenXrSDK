using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public interface IAnimationController
    {
        void Start(IAnimation animation);

        void Stop(IAnimation animation);

        void StopAll();

        IReadOnlyCollection<IAnimation> Animations { get; }

        IReferenceTime Reference { get; }
    }
}
