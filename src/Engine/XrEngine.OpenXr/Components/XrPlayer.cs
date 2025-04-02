using OpenXr.Framework;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrPlayer : Behavior<Object3D>, ITeleportHandler
    {
        Pose3 _lastPose;

        protected override void Update(RenderContext ctx)
        {
            if (XrApp.Current == null || !XrApp.Current.IsStarted) 
                return;

            var local = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace, XrApp.Current.FramePredictedDisplayTime);

            if (local != null)
            {
                if (Height == 0)
                    local.Pose.Position.Y = 0;

                //_host!.Transform.Position = local.Pose.Position;
                //_host.Transform.Orientation = local.Pose.Orientation;

                local.Pose.Orientation = local.Pose.Orientation.KeepYawOnly();

                _host!.SetWorldPose(local.Pose, false);
                _lastPose = local.Pose;
            }
        }

        public void Teleport(Vector3 position)
        {
            if (XrApp.Current == null || !XrApp.Current.IsStarted)
                return;

            var newRef = new Pose3()
            {
                Position = position,
                Orientation = Quaternion.Identity
            };

            newRef.Position.Y += Height;

            XrApp.Current.ReferenceFrame = Pose3.Identity;

            var local = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace, XrApp.Current.FramePredictedDisplayTime);

            if (local != null)
            {
                if (Height == 0)
                    local.Pose.Position.Y = 0;
                newRef.Position -= local.Pose.Position;
            }

            XrApp.Current.ReferenceFrame = newRef;

            _host!.WorldPosition = position;
        }

        public float Height { get; set; }
    }
}
