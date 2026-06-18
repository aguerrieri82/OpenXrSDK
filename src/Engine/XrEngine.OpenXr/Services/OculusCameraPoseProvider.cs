using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine.Media;
using XrMath;

namespace XrEngine.OpenXr
{
    public class OculusCameraPoseProvider : ICameraPoseProvider
    {
        public Pose3? GetCameraPose(string cameraId, long frameTime)
        {
            if (XrApp.Current == null || !XrApp.Current.IsStarted)
                return null;

            var head = XrApp.Current!.LocateSpace(XrApp.Current.Head,
                       XrApp.Current.ReferenceSpace, frameTime);

            if (!head.IsValid)
                return null;

            return head.Pose;
        }
    }
}
