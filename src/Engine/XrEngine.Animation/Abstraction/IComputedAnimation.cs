using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public interface IComputedAnimation : IAnimation
    {
        IComputeFunction Compute { get; }
    }
}
