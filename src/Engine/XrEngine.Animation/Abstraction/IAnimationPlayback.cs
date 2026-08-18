using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public enum AnimationPlaybackState
    {
        Pending,
        Playing,
        Paused,
        Completed,
        Stopped
    }

    public interface IAnimationPlayback
    {
        IAnimation Animation { get; }

        IAnimable? Host { get; }

        AnimationPlaybackState State { get; }

        float Time { get; }

        float StartRefTime { get; }

        void Play();

        void Pause();

        void Stop();

        void Seek(float t);

        void Step(float referenceTime);

        event EventHandler Updated;
    }
}
