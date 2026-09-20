using System;
using System.Collections.Generic;
using System.Text;
using XrMath;

namespace XrInteraction
{
    public struct TouchPointerStatus
    {
        public Pose3 Pose;
        public bool IsActive;
    }

    public interface ITouchPointer : IPointer
    {
        TouchPointerStatus GetPointerStatus();
    }
}
