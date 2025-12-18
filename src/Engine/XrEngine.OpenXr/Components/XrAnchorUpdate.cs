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
            XrApp? xrApp = XrApp.Current;
            xrApp?.SpacesTracker.Remove(Space);
            _isInit = false;

            base.OnDisabled();
        }

        protected override void OnEnabled()
        {
            XrApp? xrApp = XrApp.Current;

            base.OnEnabled();
        }

        protected override void Update(RenderContext ctx)
        {
            XrApp? xrApp = XrApp.Current;

            if (xrApp == null)
                return;

            if (!_isInit)
            {
                xrApp.SpacesTracker.Add(Space, UpdateInterval);
                _isInit = true;
            }

            XrSpaceLocation? loc = xrApp?.SpacesTracker.GetLastLocation(Space);

            if (loc == null || !loc.IsValid)
                return;

            _host?.SetWorldPoseIfChanged(loc.Pose, false, 0.005f);

            if (LogChanges)
            {
                float deltaPos = (loc.Pose.Position - _host!.WorldPosition).Length();
                float deltaOri = (loc.Pose.Orientation - _host!.WorldOrientation).Length();
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
