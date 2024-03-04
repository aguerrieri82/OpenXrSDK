using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;

namespace OpenXr.Framework
{
    public static class XrExtensions
    {

        public static void AddProjection(this XrLayerManager manager, RenderViewDelegate renderView)
        {
            manager.Layers.Add(new XrProjectionLayer(renderView));
        }

        public static Posef ToPoseF(this XrPose pose)
        {
            return new Posef
            {
                Orientation = pose.Orientation.ToQuaternionf(),
                Position = pose.Position.ToVector3f()
            };
        }
        
        public static Vector3f ToVector3f(this Vector3 vector)
        {
            return new Vector3f(vector.X, vector.Y, vector.Z);
        }

        public static Quaternionf ToQuaternionf(this Quaternion quat)
        {
            return new Quaternionf(quat.X, quat.Y, quat.Z, quat.W);
        }
    }
}
