using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public class XrCameraTransform
    {
        public Matrix4x4 Projection { get; set; }   

        public Matrix4x4 View { get; set; }

        public static XrCameraTransform FromView(CompositionLayerProjectionView view, float nearPlane, float farPlane)
        {
            var result = new XrCameraTransform();

            result.Projection = XrMath.CreateProjectionFov(
                   MathF.Tan(view.Fov.AngleLeft),
                   MathF.Tan(view.Fov.AngleRight),
                   MathF.Tan(view.Fov.AngleUp),
                   MathF.Tan(view.Fov.AngleDown),
                   nearPlane,
                   farPlane);

            var pose = view.Pose.ToXrPose();

            var matrix = (Matrix4x4.CreateFromQuaternion(pose.Orientation) *
                          Matrix4x4.CreateTranslation(pose.Position));

            result.View = matrix.InvertRigidBody();

            return result;
        }
    }
}
