﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine.OpenXr
{
    public interface ITeleportTarget
    {
        bool CanTeleport(Vector3 point);

        IEnumerable<float> GetYPlanes();
    }
}
