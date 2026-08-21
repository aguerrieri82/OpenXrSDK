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

            var head = XrApp.Current.SpacesTracker.GetLastLocation(XrApp.Current.Head);

            if (head != null && head.IsValid)
            {
                if (Height == 0)
                    head.Pose.Position.Y = 0;

                head.Pose.Orientation = head.Pose.Orientation.KeepYawOnly();

                _host!.SetWorldPose(head.Pose, false);
                _lastPose = head.Pose;
            }
        }

        public void Teleport(Vector3 position)
        {
            var app = XrApp.Current;

            if (app == null || !app.IsStarted)
                return;

            var targetHeadPosition = position;
            targetHeadPosition.Y += Height;

            var oldRef = app.ReferenceFrame;

            var newRef = new Pose3
            {
                Position = targetHeadPosition,
                Orientation = Quaternion.Identity
            };

            var head = app.SpacesTracker.GetLastLocation(app.Head);

            if (head != null && head.IsValid)
            {
                var rawHeadPose = oldRef.Inverse().Multiply(head.Pose);

                var rawHeadPosition = rawHeadPose.Position;

                if (Height == 0)
                    rawHeadPosition.Y = 0;

                newRef.Position = targetHeadPosition - rawHeadPosition;
            }

            app.ReferenceFrame = newRef;

            _host!.WorldPosition = position;
        }

        public float Height { get; set; }
    }
}
