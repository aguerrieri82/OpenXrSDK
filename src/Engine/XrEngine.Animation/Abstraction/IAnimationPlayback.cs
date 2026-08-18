using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public interface IAnimationPlayback
    {
        void Stop();

        IAnimation Animation { get; }

        IAnimable Host { get; }

        float Time { get; }
        
        float StartTime { get; }
        
        bool IsStarted { get; }

        bool IsCompleted { get; }
    }
}
