using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class SpatialAnchorGrid2 : AsyncBehavior<Object3D>, IDisposable
    {
        public class SpatialAnchor
        {
            public XrSpatialEntity Entity;
            public Guid Id;
            public Uuid PersistUuid;
            public Pose3 LocalPose;
            public Pose3 CurrentWorldPose;
            public bool IsCreated;
            public bool IsPersisted;
            public bool IsTracking;
        }

        protected XrSpatial? _spatial;
        protected readonly List<SpatialAnchor> _anchors = [];
        protected readonly List<(SpatialAnchor Anchor, float Distance)> _changedAnchors = [];
        protected Pose3 _lastPose;
        protected double _lastCheckTime = double.NegativeInfinity;
        protected bool _isInit;
        protected bool _isBusy;
        protected bool _isDisposed;
        protected bool _discoveryRecommended;

        public SpatialAnchorGrid2()
        {
            CheckThreshold = 0.2f;
            MaxDistance = 2f;
            UpdateIntervalSec = 0.1f;
            DistanceTollerance = 0.01f;
        }

        public SpatialAnchor? GetClosestAnchor(Vector3 worldPos, out float distance)
        {
            SpatialAnchor? result = null;
            distance = float.PositiveInfinity;

            foreach (var anchor in _anchors)
            {
                if (!anchor.IsTracking)
                    continue;

                var curDistance = Vector3.Distance(anchor.CurrentWorldPose.Position, worldPos);
                if (curDistance < distance)
                {
                    distance = curDistance;
                    result = anchor;
                }
            }
            return result;
        }

        protected async Task InitializeAsync(XrApp app)
        {
            _spatial = await app.Plugin<XrOculusPlugin>().GetSpatialAsync();
            if (!_spatial.EnumerateCapabilities().Contains(SpatialCapabilityEXT.AnchorExt))
                throw new NotSupportedException("Spatial anchors are not supported");

            if (IsPersistent && !_spatial.HasPersistenceScope(SpatialPersistenceScopeEXT.LocalAnchorsExt))
                throw new NotSupportedException("Local spatial anchor persistence is not supported");

            if (IsPersistent && !_isDisposed)
                await LoadAnchorsAsync();

            _spatial.DiscoveryRecommended += OnDiscoveryRecommended;
            _isInit = true;
        }

        protected async Task LoadAnchorsAsync()
        {
            var snapshot = await _spatial!.DiscoverAsync([SpatialComponentTypeEXT.AnchorExt, SpatialComponentTypeEXT.PersistenceExt]);
            try
            {
                await EngineApp.MainThread;
                if (_isDisposed)
                    return;

                var locations = _spatial.GetAnchors(snapshot);
                var persistence = _spatial.GetPersistenceData(snapshot);

                for (var i = 0; i < locations.EntityIds.Length; i++)
                {
                    if (_anchors.Any(a => a.Entity.Id == locations.EntityIds[i]))
                        continue;

                    if (locations.EntityStates[i] != SpatialEntityTrackingStateEXT.TrackingExt)
                        continue;

                    var index = Array.IndexOf(persistence.EntityIds, locations.EntityIds[i]);
                    if (index < 0 || persistence.Data[index].PersistState != SpatialPersistenceStateEXT.LoadedExt)
                        continue;

                    var entity = _spatial.CreateEntity(locations.EntityIds[i]);
                    AddAnchor(entity, persistence.Data[index].PersistUuid, locations.Data[i].ToPose3(), false, true);
                }
            }
            finally
            {
                _spatial.DestroySnapshot(ref snapshot);
            }
        }

        protected unsafe void AddAnchor(XrSpatialEntity entity, Uuid uuid, Pose3 worldPose, bool isCreated, bool isPersisted)
        {
            _anchors.Add(new SpatialAnchor
            {
                Entity = entity,
                Id = isPersisted ? new Guid(new ReadOnlySpan<byte>(uuid.Data, 16)) : Guid.Empty,
                PersistUuid = uuid,
                LocalPose = _host.GetWorldPose().Inverse().Multiply(worldPose),
                CurrentWorldPose = worldPose,
                IsCreated = isCreated,
                IsPersisted = isPersisted,
                IsTracking = true
            });
        }

        protected override async Task UpdateAsync(RenderContext ctx)
        {
            var app = XrApp.Current;
            if (_isDisposed || _isBusy || app == null || !app.IsStarted)
                return;

            if (ctx.Time - _lastCheckTime < UpdateIntervalSec)
                return;

            _isBusy = true;
            try
            {
                if (_isInit && !_spatial!.IsCreated)
                {
                    ReleaseSpatial();
                    _isInit = false;
                }
                if (!_isInit)
                    await InitializeAsync(app);

                if (_discoveryRecommended && IsPersistent)
                {
                    _discoveryRecommended = false;
                    await LoadAnchorsAsync();
                }

                await EngineApp.MainThread;
                if (_isDisposed)
                    return;

                _lastCheckTime = ctx.Time;
                var head = app.SpacesTracker.GetLastLocation(app.Head);
                if (head == null || !head.IsValid)
                    return;

                UpdateAnchors(head.Pose);

                if (IsPersistent)
                {
                    foreach (var anchor in _anchors)
                    {
                        if (!anchor.IsCreated || anchor.IsPersisted || !anchor.IsTracking)
                            continue;

                        var uuid = await _spatial!.PersistAsync(anchor.Entity.Id);
                        await EngineApp.MainThread;
                        SetPersistence(anchor, uuid);
                        if (_isDisposed)
                            return;
                    }
                }

                if (_anchors.Count == 0 || Vector3.Distance(_lastPose.Position, head.Pose.Position) > CheckThreshold)
                {
                    var closest = GetClosestAnchor(head.Pose.Position, out var distance);
                    if (closest == null || distance > MaxDistance)
                    {
                        var entity = _spatial!.CreateAnchor(head.Pose);
                        try
                        {
                            // Persist on the next update, after the runtime reports tracking.
                            AddAnchor(entity, default, head.Pose, true, false);
                            entity = default;
                        }
                        finally
                        {
                            if (entity.Handle.Handle != 0)
                                _spatial.DestroyEntity(ref entity);
                        }
                    }
                }

                _lastPose = head.Pose;
            }
            finally
            {
                _isBusy = false;
                if (_isDisposed)
                    ReleaseSpatial();
            }
        }

        protected void UpdateAnchors(Pose3 headPose)
        {
            if (_anchors.Count == 0)
                return;

            var snapshot = _spatial!.CreateUpdateSnapshot(_anchors.Select(a => a.Entity).ToArray(), [SpatialComponentTypeEXT.AnchorExt]);
            try
            {
                var locations = _spatial.GetAnchors(snapshot);
                _changedAnchors.Clear();

                foreach (var anchor in _anchors)
                {
                    var index = Array.IndexOf(locations.EntityIds, anchor.Entity.Id);
                    anchor.IsTracking = index >= 0 && locations.EntityStates[index] == SpatialEntityTrackingStateEXT.TrackingExt;
                    if (!anchor.IsTracking)
                        continue;

                    var pose = locations.Data[index].ToPose3();
                    var offset = Vector3.Distance(pose.Position, anchor.CurrentWorldPose.Position);
                    var distance = Vector3.Distance(pose.Position, headPose.Position);

                    anchor.CurrentWorldPose = pose;

                    if (offset > DistanceTollerance && distance <= MaxDistance)
                        _changedAnchors.Add((anchor, distance));
                }

                if (_changedAnchors.Count > 0)
                {
                    var (anchor, _) = _changedAnchors.MinBy(a => a.Distance);
                    _host.SetWorldPose(anchor.CurrentWorldPose.Multiply(anchor.LocalPose.Inverse()));
                }
            }
            finally
            {
                _spatial.DestroySnapshot(ref snapshot);
            }
        }

        public async Task ClearAsync(bool delete)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpatialAnchorGrid2));
            if (_isBusy)
                throw new InvalidOperationException("Spatial anchor grid is updating");

            _isBusy = true;
            try
            {
                while (_anchors.Count > 0)
                {
                    var anchor = _anchors[^1];
                    if (delete && anchor.IsPersisted)
                        await _spatial!.UnpersistAsync(anchor.PersistUuid);

                    _spatial!.DestroyEntity(ref anchor.Entity);
                    _anchors.RemoveAt(_anchors.Count - 1);
                }
                _lastPose = default;
            }
            finally
            {
                _isBusy = false;
                if (_isDisposed)
                    ReleaseSpatial();
            }
        }

        protected void ReleaseSpatial()
        {
            if (_spatial != null)
                _spatial.DiscoveryRecommended -= OnDiscoveryRecommended;
            if (_spatial != null && _spatial.IsCreated)
            {
                foreach (var anchor in _anchors)
                    _spatial.DestroyEntity(ref anchor.Entity);
            }
            _spatial = null;
            _anchors.Clear();
            _changedAnchors.Clear();
        }

        protected void OnDiscoveryRecommended()
        {
            _discoveryRecommended = true;
        }

        protected unsafe void SetPersistence(SpatialAnchor anchor, Uuid uuid)
        {
            anchor.PersistUuid = uuid;
            anchor.Id = new Guid(new ReadOnlySpan<byte>(uuid.Data, 16));
            anchor.IsPersisted = true;
        }

        public void Dispose()
        {
            _isDisposed = true;
            if (!_isBusy)
                ReleaseSpatial();
            GC.SuppressFinalize(this);
        }

        public float CheckThreshold { get; set; }

        public float MaxDistance { get; set; }

        // Configure before the first update. Persistence uses the local anchor store.
        public bool IsPersistent { get; set; }

        public float UpdateIntervalSec { get; set; }

        public float DistanceTollerance { get; set; }
    }
}
