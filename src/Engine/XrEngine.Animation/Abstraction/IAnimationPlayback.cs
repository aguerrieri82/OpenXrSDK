using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public interface IAnimationPlayback
    {
        void Stop();

        void Seek(float t);

        IAnimation Animation { get; }

        IAnimable Host { get; }

        float Time { get; }
        
        float StartRefTime { get; }
        
        bool IsStarted { get; }

        bool IsCompleted { get; }
    }
}
