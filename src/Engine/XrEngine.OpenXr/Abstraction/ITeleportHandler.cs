using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine.OpenXr
{
    public interface ITeleportHandler
    {
        void Teleport(Vector3 position);    
    }
}
