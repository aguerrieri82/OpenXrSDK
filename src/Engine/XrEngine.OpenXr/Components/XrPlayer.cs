using OpenXr.Framework;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrPlayer : Behavior<Object3D>
    {
        Pose3 _lastPose;

        protected override void Update(RenderContext ctx)
        {
            if (XrApp.Current == null)
                return;

            var newPose = _host!.GetWorldPose();
            newPose.Position.Y += Height;

            if (!_lastPose.IsSimilar(newPose))
            {
                _lastPose = newPose;

                if (XrApp.Current != null && XrApp.Current.IsStarted)
                {
                    XrApp.Current.ReferenceFrame = Pose3.Identity;

                    var local = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace, XrApp.Current.FramePredictedDisplayTime);

                    if (local != null)
                    {
                        if (Height == 0)
                            local.Pose.Position.Y = 0;
                        newPose.Position -= local.Pose.Position;
                    }


                    XrApp.Current.ReferenceFrame = newPose;
                }
            }
        }


        public float Height { get; set; }
    }
}
