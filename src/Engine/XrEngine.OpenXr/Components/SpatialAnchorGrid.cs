using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class SpatialAnchorGrid : BaseXrComponent<Object3D>
    {
        public class SpatialAnchor
        {
            public Space Space;

            public Guid Id;

            public Pose3 LocalPose;

            public Pose3 CurrentWorldPose;

            public bool IsCreated;
        }

        protected Pose3 _lastPose;
        protected XrOculusPlugin? _oculus;
        protected readonly List<SpatialAnchor> _anchors = [];
        protected bool _isLoaded;
        protected List<(SpatialAnchor Anchor, float Distance)> _changedAnchors = [];

        public SpatialAnchorGrid()
        {
            CheckThreshold = 0.2f;
            MaxDistance = 2f;
            UpdateIntervalSec = 0.1f;
            DistanceTollerance = 0.01f;
            _isAsync = true;
        }

        public SpatialAnchor? GetClosestAnchor(Vector3 worldPos, out float distance)
        {
            SpatialAnchor? minAnchor = null;
            var minDistance = float.PositiveInfinity;

            foreach (var anchor in _anchors)
            {
                var curDist = (anchor.CurrentWorldPose.Position - worldPos).Length();
                if (curDist < minDistance)
                {
                    minDistance = curDist;
                    minAnchor = anchor;
                }
            }

            distance = minDistance;

            return minAnchor;
        }

        public async Task ClearAsync(bool delete)
        {
            Debug.Assert(_oculus != null);
            Debug.Assert(_xrApp != null);

            foreach (var anchor in _anchors)
            {
                _xrApp.SpacesTracker.Remove(anchor.Space);

                if (delete)
                {
                    if (anchor.IsCreated)
                        _xrApp.DestroySpace(anchor.Space);

                    if (_oculus.GetSpaceComponentEnabled(anchor.Space, SpaceComponentTypeFB.StorableFB))
                    {
                        var supported = _oculus.EnumerateSpaceSupportedComponentsFB(anchor.Space);

                        if (!supported.Contains(SpaceComponentTypeFB.RoomLayoutFB) &&
                            !supported.Contains(SpaceComponentTypeFB.SemanticLabelsFB))
                            await _oculus.EraseSpaceAsync(anchor.Space, true);
                    }

                }
            }

            _anchors.Clear();
        }

        protected async Task LoadAnchorsAsync()
        {
            Debug.Assert(_oculus != null);
            Debug.Assert(_xrApp != null);

            await ClearAsync(false);

            var spaces = await _oculus.DiscoverSpacesAsync();

            foreach (var anchor in spaces)
            {
                var supported = _oculus.EnumerateSpaceSupportedComponentsFB(anchor.Space);

                if (!supported.Contains(SpaceComponentTypeFB.LocatableFB))
                    continue;

                await _oculus.EnsureSpaceComponentAsync(anchor.Space, SpaceComponentTypeFB.LocatableFB);

                var pose = _xrApp.LocateSpace(anchor.Space, _xrApp.ReferenceSpace);

                if (pose.IsValid)
                    AddAnchor(anchor.Space, anchor.Uuid.ToGuid(), pose.Pose, false);
            }

        }

        protected void AddAnchor(Space space, Guid id, Pose3 worldPose, bool isCreated)
        {
            Debug.Assert(_xrApp != null);

            var hostWorldPose = _host.GetWorldPose();

            _anchors.Add(new SpatialAnchor
            {
                Id = id,
                Space = space,
                CurrentWorldPose = worldPose,
                LocalPose = hostWorldPose.Inverse().Multiply(worldPose),
                IsCreated = isCreated
            });

            _xrApp.SpacesTracker.Add(space, TimeSpan.FromSeconds(UpdateIntervalSec));
        }

        protected override void AttachXr()
        {
            _oculus ??= _xrApp!.Plugin<XrOculusPlugin>();
        }

        protected override void DetachXr()
        {
            _oculus = null;
            _isLoaded = false;
        }

        protected override async Task UpdateWorkAsync(RenderContext ctx)
        {
            Debug.Assert(_xrApp != null);

            Debug.Assert(_oculus != null);

            if (!_isLoaded)
            {
                await LoadAnchorsAsync();
                _isLoaded = true;
            }

            var head = _xrApp.SpacesTracker.GetLastLocation(_xrApp.Head);

            if (head == null || !head.IsValid)
                return;

            if ((_lastPose.Position - head.Pose.Position).Length() > CheckThreshold)
            {
                var anchor = GetClosestAnchor(head.Pose.Position, out var distance);

                if (anchor == null || distance > MaxDistance)
                {
                    var xrAnchor = await _oculus.CreateAnchorAsync(head.Pose, _xrApp.ReferenceSpace);

                    await _oculus.EnsureSpaceComponentAsync(xrAnchor.Space, SpaceComponentTypeFB.LocatableFB);

                    if (IsPersistent)
                        await _oculus.SaveSpaceAsync(xrAnchor.Space, true);

                    AddAnchor(xrAnchor.Space, xrAnchor.Id, head.Pose, true);
                }
            }

            var hostWorldPose = _host.GetWorldPose();

            _changedAnchors.Clear();

            foreach (var anchor in _anchors)
            {
                var location = _xrApp.SpacesTracker.GetLastLocation(anchor.Space);

                if (location == null || !location.IsValid)
                    continue;

                var offset = (location.Pose.Position - anchor.CurrentWorldPose.Position).Length();

                var headDistance = (location.Pose.Position - head.Pose.Position).Length();

                anchor.CurrentWorldPose = location.Pose;

                if (offset > DistanceTollerance && headDistance <= MaxDistance)
                    _changedAnchors.Add((anchor, headDistance));
            }

            if (_changedAnchors.Count > 0)
            {
                var (anchor, _) = _changedAnchors.MinBy(a => a.Distance);

                var newHostPose = anchor.CurrentWorldPose.Multiply(anchor.LocalPose.Inverse());

                _host.SetWorldPose(newHostPose);
            }

            _lastPose = head.Pose;
        }

        public float CheckThreshold { get; set; }

        public float MaxDistance { get; set; }

        public bool IsPersistent { get; set; }

        public float UpdateIntervalSec { get; set; }

        public float DistanceTollerance { get; set; }
    }
}
