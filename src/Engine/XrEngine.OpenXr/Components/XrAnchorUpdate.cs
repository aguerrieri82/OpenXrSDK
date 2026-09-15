using OpenXr.Framework;
using Silk.NET.OpenXR;

namespace XrEngine.OpenXr
{

    public class XrAnchorUpdate : BaseXrComponent<Object3D>
    {
        protected bool _hasPose;

        protected override void AttachXr()
        {
            if (IsEnabled && Space.Handle != 0)
                _xrApp!.SpacesTracker.Add(Space, UpdateInterval);
        }

        protected override void DetachXr()
        {
            _xrApp!.SpacesTracker.Remove(Space);
            Space = new Space();
        }

        protected override void OnEnabled()
        {
            if (_xrApp != null)
                AttachXr();
        }

        protected override void OnDisabled()
        {
            if (_xrApp != null)
                DetachXr();
        }

        protected override void UpdateWork(RenderContext ctx)
        {
            if (Space.Handle == 0)
                return;

            var loc = _xrApp!.SpacesTracker.GetLastLocation(Space);

            if (loc == null || !loc.IsValid)
                return;

            _hasPose = true;

            _host!.SetWorldPoseIfChanged(loc.Pose, false, 0.005f);

            if (LogChanges)
            {
                var deltaPos = (loc.Pose.Position - _host.WorldPosition).Length();
                
                var deltaOri = (loc.Pose.Orientation - _host.WorldOrientation).Length();

                if (deltaPos > 0.005 || deltaOri > 0.005)
                    Log.Debug(this, $"{_host.Name} DP: {deltaPos} - DO: {deltaOri}");
            }
        }

        public TimeSpan UpdateInterval { get; set; }

        public Guid AnchorId { get; set; }

        public Space Space { get; set; }

        public bool LogChanges { get; set; }

        public bool HasPose => _hasPose;
    }
}
