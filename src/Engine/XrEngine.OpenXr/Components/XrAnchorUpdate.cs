using OpenXr.Framework;
using Silk.NET.OpenXR;

namespace XrEngine.OpenXr
{

    public class XrAnchorUpdate : Behavior<Object3D>
    {
        protected bool _isInit;

        protected override void Start(RenderContext ctx)
        {
            OnEnabled();
        }

        protected override void OnDisabled()
        {
            var xrApp = XrApp.Current;
            xrApp?.SpacesTracker.Remove(Space);
            _isInit = false;

            base.OnDisabled();
        }

        protected override void OnEnabled()
        {
            var xrApp = XrApp.Current;

            base.OnEnabled();
        }

        protected override void Update(RenderContext ctx)
        {
            var xrApp = XrApp.Current;

            if (xrApp == null)
                return;

            if (!_isInit)
            {
                xrApp.SpacesTracker.Add(Space, UpdateInterval);
                _isInit = true;
            }

            var loc = xrApp?.SpacesTracker.GetLastLocation(Space);

            if (loc == null || !loc.IsValid)
                return;

            _host?.SetWorldPoseIfChanged(loc.Pose, true, 0.005f);

            if (LogChanges)
            {
                var deltaPos = (loc.Pose.Position - _host!.WorldPosition).Length();
                var deltaOri = (loc.Pose.Orientation - _host!.WorldOrientation).Length();
                if (deltaPos > 0.005 || deltaOri > 0.005)
                    Log.Debug(this, $"{_host.Name} DP: {deltaPos} - DO: {deltaOri}");
            }
        }

        public TimeSpan UpdateInterval { get; set; }

        public Guid AnchorId { get; set; }

        public Space Space { get; set; }

        public bool LogChanges { get; set; }
    }
}
