using System;
using System.Collections.Generic;
using System.Text;

namespace XrInteraction
{
    public interface IPointer
    {
        int PointerId { get; }

        string Name { get; }

    }
}
