using OpenXr.Framework;
using XrEngine;
using XrEngine.Components;
using XrMath;

namespace XrEngine.OpenXr
{

    public class XrCameraPlayer : BaseFramePlayer<TransformRecorder.TransformRecordFrame, Camera>
    {
        private bool _isFirstLoad = true;
        private Pose3 _headOffset;

        public XrCameraPlayer()
        {
            SourceFile = "transform.json";
        }

        protected override void OnLoopStart()
        {
            Log.Warn(this, "Loop START");
        }

        protected override void ApplyFrame(TransformRecorder.TransformRecordFrame frame)
        {
            if (XrApp.Current == null)
                return;

            var framePose = frame.WorldMatrix.ToPose();

            if (_isFirstLoad)
            {
                var headPose = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace);

                if (!headPose.IsValid)
                    return;

                _headOffset = headPose.Pose.Inverse();
                _isFirstLoad = false;
            }

            XrApp.Current.ReferenceFrame = framePose.Multiply(_headOffset);
        }
    }
}