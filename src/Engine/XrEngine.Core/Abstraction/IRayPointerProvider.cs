using System;
using System.Collections.Generic;
using System.Text;
using XrInteraction;

namespace XrEngine
{
    public interface IRayHitTestSource
    {
        HitTestResult LastHit { get; }
    }

    public interface IRayPointerProvider : IComponent
    {
        IRayPointer? Pointer { get; }

        void SetHitTestSource(IRayHitTestSource source);
    }
}
