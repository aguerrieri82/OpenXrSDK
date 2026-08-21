using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine
{
    public interface IWorldLocatable
    {
        Vector3 WorldPosition { get; set; }

        Quaternion WorldOrientation { get; set; }
    }
}
