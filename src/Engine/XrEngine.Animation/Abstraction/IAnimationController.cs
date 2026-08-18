using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public interface IAnimationController
    {
        IAnimationPlayback Start(IAnimation animation, IAnimable? host = null);

        void Stop(IAnimationPlayback playback);

        void Seek(IAnimationPlayback playback, float t);

        void StopAll();

        void Step(IAnimationPlayback playback);

        IReadOnlyCollection<IAnimationPlayback> ActiveAnimations { get; }

        IReferenceTime Reference { get; }
    }
}
