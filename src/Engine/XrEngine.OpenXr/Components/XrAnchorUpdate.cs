using OpenXr.Framework;
using Silk.NET.OpenXR;

namespace XrEngine.OpenXr.Components
{
    [Obsolete("Schedule anchor updates")]
    public class XrAnchorUpdate : Behavior<Object3D>
    {
        double _lastUpdateTime;

        protected override void Start(RenderContext ctx)
        {
            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            var xrApp = XrApp.Current;

            if (xrApp == null || !xrApp.IsStarted)
                return;

            if (ctx.Time - _lastUpdateTime < UpdateInterval.TotalSeconds)
                return;

            var loc = xrApp.LocateSpace(Space, xrApp.Stage, xrApp.FramePredictedDisplayTime);
            if (loc.IsValid)
            {
                if (LogChanges)
                {
                    var deltaPos = (loc.Pose.Position - _host!.WorldPosition).Length();
                    var deltaOri = (loc.Pose.Orientation - _host!.WorldOrientation).Length();
                    if (deltaPos > 0.005 || deltaOri > 0.005)
                        Log.Debug(this, $"{_host.Name} DP: {deltaPos} - DO: {deltaOri}");
                }
                _host?.SetGlobalPoseIfChanged(loc.Pose, 0.005f);
            }


            _lastUpdateTime = ctx.Time;

            base.Update(ctx);
        }

        public TimeSpan UpdateInterval { get; set; }

        public Guid AnchorId { get; set; }

        public Space Space { get; set; }

        public bool LogChanges { get; set; }
    }
}
