using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class OculusSceneView2 : Group3D
    {
        protected class SceneEntity
        {
            public XrSpatialEntity Entity;
            public Object3D Model = null!;
            public SceneModelInfo Info = null!;
        }

        protected readonly Dictionary<ulong, SceneEntity> _entities = [];
        protected XrApp? _app;
        protected XrSpatial? _spatial;
        protected SpatialComponentTypeEXT[] _spatialComponents = [];
        protected bool _isSceneLoaded;
        protected bool _isSceneLoading;
        protected bool _discoveryRecommended;
        protected bool _isDisposed;
        protected double _lastPoseTime;

        public OculusSceneView2()
        {
            Flags |= EngineObjectFlags.DisableNotifyChangedScene | EngineObjectFlags.Generated;
            Name = "SceneView2";
            Factory = DefaultSceneModelFactory.Instance;
            UpdateInterval = TimeSpan.FromMilliseconds(300);
        }

        public override void Update(RenderContext ctx)
        {
            if (_isDisposed)
                return;

            _app ??= XrApp.Current;

            if (_app != null && _app.IsStarted)
            {
                if (_isSceneLoaded && !_isSceneLoading && !_spatial!.IsCreated)
                {
                    ReleaseSpatial();
                    _isSceneLoaded = false;
                }

                if (!_isSceneLoading && (!_isSceneLoaded || _discoveryRecommended))
                    _ = LoadSceneAsync();

                if (_isSceneLoaded && !_isSceneLoading && ctx.Time - _lastPoseTime >= UpdateInterval.TotalSeconds)
                {
                    UpdatePoses();
                    _lastPoseTime = ctx.Time;
                }
            }
            base.Update(ctx);
        }

        public Object3D? AddChild(SceneModelInfo model)
        {
            var obj = Factory.CreateModel(model);
            if (obj != null)
                AddChild(obj);
            return obj;
        }

        protected async Task InitializeAsync()
        {
            _spatial = await _app!.Plugin<XrOculusPlugin>().GetSpatialAsync();

            var supported = _spatial.EnumerateComponents(SpatialCapabilityEXT.PlaneTrackingExt);
            
            if (!supported.Contains(SpatialComponentTypeEXT.Bounded2DExt))
                throw new NotSupportedException("Spatial planes do not provide 2D bounds");

            var components = new List<SpatialComponentTypeEXT> { SpatialComponentTypeEXT.Bounded2DExt };
            
            if (supported.Contains(SpatialComponentTypeEXT.PlaneSemanticLabelExt))
                components.Add(SpatialComponentTypeEXT.PlaneSemanticLabelExt);

            if (_spatial.IsPersistenceCreated && supported.Contains(SpatialComponentTypeEXT.PersistenceExt))
                components.Add(SpatialComponentTypeEXT.PersistenceExt);

            _spatialComponents = components.ToArray();

            _spatial.DiscoveryRecommended += OnDiscoveryRecommended;
        }

        protected async Task LoadSceneAsync()
        {
            _isSceneLoading = true;
            _discoveryRecommended = false;
            try
            {
                if (_spatial == null)
                    await InitializeAsync();

                if (_isDisposed)
                    return;

                var snapshot = await _spatial!.DiscoverAsync(_spatialComponents);

                try
                {
                    await EngineApp.MainThread;

                    if (_isDisposed)
                        return;

                    ReadScene(snapshot);

                    _isSceneLoaded = true;
                }
                finally
                {
                    _spatial.DestroySnapshot(ref snapshot);
                }

                SceneReady?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                if (!_isSceneLoaded)
                    ReleaseSpatial();

                Log.Error(this, ex, "Load spatial scene");
            }
            finally
            {
                _isSceneLoading = false;
                if (_isDisposed)
                    ReleaseSpatial();
            }
        }

        protected unsafe void ReadScene(SpatialSnapshotEXT snapshot)
        {
            var bounds = _spatial!.GetBounds2D(snapshot);
            var labels = _spatialComponents.Contains(SpatialComponentTypeEXT.PlaneSemanticLabelExt) ? _spatial.GetPlaneSemanticLabels(snapshot) : null;
            var persistence = _spatialComponents.Contains(SpatialComponentTypeEXT.PersistenceExt) ? _spatial.GetPersistenceData(snapshot) : null;

            for (var i = 0; i < bounds.EntityIds.Length; i++)
            {
                var id = bounds.EntityIds[i];
                if (bounds.EntityStates[i] != SpatialEntityTrackingStateEXT.TrackingExt)
                    continue;

                if (_entities.ContainsKey(id))
                    continue;

                var labelIndex = labels == null ? -1 : Array.IndexOf(labels.EntityIds, id);
                var label = labelIndex < 0 ? SpatialPlaneSemanticLabelEXT.UncategorizedExt : labels!.Data[labelIndex];
                var info = new SceneModelInfo
                {
                    Pose = bounds.Data[i].Center.ToPose3(),
                    Size = new Vector2(bounds.Data[i].Extents.Width, bounds.Data[i].Extents.Height)
                };

                switch (label)
                {
                    case SpatialPlaneSemanticLabelEXT.WallExt:
                        info.Type = SceneModelType.Wall;
                        info.Labels = ["WALL_FACE"];
                        break;
                    case SpatialPlaneSemanticLabelEXT.FloorExt:
                        info.Type = SceneModelType.Floor;
                        info.Labels = ["FLOOR"];
                        break;
                    case SpatialPlaneSemanticLabelEXT.CeilingExt:
                        info.Type = SceneModelType.Ceiling;
                        info.Labels = ["CEILING"];
                        break;
                    case SpatialPlaneSemanticLabelEXT.TableExt:
                        info.Labels = ["TABLE"];
                        break;
                    default:
                        info.Labels = ["OTHER"];
                        break;
                }

                var persistIndex = persistence == null ? -1 : Array.IndexOf(persistence.EntityIds, id);
                if (persistIndex >= 0 && persistence!.Data[persistIndex].PersistState == SpatialPersistenceStateEXT.LoadedExt)
                {
                    var uuid = persistence.Data[persistIndex].PersistUuid;
                    info.AnchorId = new Guid(new ReadOnlySpan<byte>(uuid.Data, 16));
                }

                // EXT entities are not XrSpace handles. SceneModelInfo.Space stays empty.
                var model = Factory.CreateModel(info);
                if (model == null)
                    continue;

                var entity = _spatial.CreateEntity(id);
                _entities.Add(id, new SceneEntity { Entity = entity, Model = model, Info = info });
                AddChild(model);
            }
        }

        protected void UpdatePoses()
        {
            if (_entities.Count == 0)
                return;

            var handles = _entities.Values.Select(a => a.Entity).ToArray();
            var snapshot = _spatial!.CreateUpdateSnapshot(handles, [SpatialComponentTypeEXT.Bounded2DExt]);

            try
            {
                var bounds = _spatial.GetBounds2D(snapshot);

                foreach (var item in _entities.Values)
                {
                    var index = Array.IndexOf(bounds.EntityIds, item.Entity.Id);
                    item.Model.IsVisible = index >= 0 && bounds.EntityStates[index] == SpatialEntityTrackingStateEXT.TrackingExt;
                    
                    if (!item.Model.IsVisible)
                        continue;

                    item.Info.Pose = bounds.Data[index].Center.ToPose3();
                    
                    var size = new Vector2(bounds.Data[index].Extents.Width, bounds.Data[index].Extents.Height);
                    
                    if (size != item.Info.Size && item.Info.Size.X > 0 && item.Info.Size.Y > 0)
                        item.Model.Transform.Scale *= new Vector3(size.X / item.Info.Size.X, size.Y / item.Info.Size.Y, 1);

                    item.Info.Size = size;
                    item.Model.SetWorldPoseIfChanged(item.Info.Pose, false, 0.005f);
                }
            }
            finally
            {
                _spatial.DestroySnapshot(ref snapshot);
            }
        }

        protected void OnDiscoveryRecommended()
        {
            _discoveryRecommended = true;
        }

        protected void ReleaseSpatial()
        {
            if (_spatial != null)
            {
                _spatial.DiscoveryRecommended -= OnDiscoveryRecommended;

                if (_spatial.IsCreated)
                {
                    foreach (var item in _entities.Values)
                        _spatial.DestroyEntity(ref item.Entity);
                }
                
                _spatial = null;
            }

            foreach (var item in _entities.Values)
            {
                RemoveChild(item.Model);
                item.Model.Dispose();
            }
            
            _entities.Clear();
        }

        public override void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (!_isSceneLoading)
                ReleaseSpatial();
            base.Dispose();
        }

        public TimeSpan UpdateInterval { get; set; }

        public ISceneModelFactory Factory { get; set; }

        public event EventHandler? SceneReady;
    }
}
