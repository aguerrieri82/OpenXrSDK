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
            if (XrApp.Current == null || !XrApp.Current.IsStarted)
                return;

            var newRef = new Pose3()
            {
                Position = position,
                Orientation = Quaternion.Identity
            };

            newRef.Position.Y += Height;

            XrApp.Current.ReferenceFrame = Pose3.Identity;

            var head = XrApp.Current.SpacesTracker.GetLastLocation(XrApp.Current.Head);

            if (head != null && head.IsValid)
            {
                if (Height == 0)
                    head.Pose.Position.Y = 0;
                newRef.Position -= head.Pose.Position;
            }

            XrApp.Current.ReferenceFrame = newRef;

            _host!.WorldPosition = position;
        }

        public float Height { get; set; }
    }
}
