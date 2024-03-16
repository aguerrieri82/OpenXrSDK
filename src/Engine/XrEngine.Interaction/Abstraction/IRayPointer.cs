using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.Interaction
{
    public struct RayPointerStatus
    {
        public Ray3 Ray;

        public PointerButton Buttons;

        public bool IsActive;
    }

    public interface IRayPointer
    {
        RayPointerStatus GetPointerStatus();
    }
}
