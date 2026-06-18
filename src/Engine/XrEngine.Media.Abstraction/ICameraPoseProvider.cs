using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine.Media
{
    public interface ICameraPoseProvider
    {
        Pose3? GetCameraPose(string cameraId, long frameTime);
    }
}
