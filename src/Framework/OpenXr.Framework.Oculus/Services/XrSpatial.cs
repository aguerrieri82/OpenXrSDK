using Common.Interop;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.EXT;
using System.Text;
using XrMath;

namespace OpenXr.Framework.Oculus
{
    public class XrSpatialCapability
    {
        public SpatialCapabilityEXT Capability;

        // Null enables all components reported for this capability.
        public SpatialComponentTypeEXT[]? Components;
        public SpatialMarkerArucoDictEXT ArUcoDictionary;
        public SpatialMarkerAprilTagDictEXT AprilTagDictionary;
        public float? MarkerSideLength;
        public bool? OptimizeForStaticMarker;
    }

    public struct XrSpatialEntity
    {
        public ulong Id;
        public SpatialEntityEXT Handle;
    }

    public class XrSpatialQueryResult
    {
        public ulong[] EntityIds = [];
        public SpatialEntityTrackingStateEXT[] EntityStates = [];
    }

    public class XrSpatialComponentData<T> : XrSpatialQueryResult
    {
        // Data follows EntityIds order. Read it only for tracking entities,
        // except persistence data, which is valid in every tracking state.
        public T[] Data = [];
    }

    public class XrSpatial : IDisposable
    {
        readonly XrApp _app;
        readonly ExtSpatialEntity _ext;
        readonly ExtSpatialAnchor? _anchor;
        readonly ExtSpatialPersistence? _persistence;
        readonly ExtSpatialPersistenceOperations? _persistenceOperations;

        SpatialContextEXT _context;
        readonly Dictionary<SpatialPersistenceScopeEXT, SpatialPersistenceContextEXT> _persistenceContexts = [];
        readonly Session _session;
        int _pendingOperations;
        bool _disposed;

        public XrSpatial(XrApp app)
        {
            _app = app;
            _session = app.Session;

            if (!app.Xr.TryGetInstanceExtension<ExtSpatialEntity>(null, app.Instance, out var ext))
                throw new NotSupportedException(ExtSpatialEntity.ExtensionName);

            _ext = ext;

            if (app.HasExtension(ExtSpatialAnchor.ExtensionName))
                app.Xr.TryGetInstanceExtension(null, app.Instance, out _anchor);

            if (app.HasExtension(ExtSpatialPersistence.ExtensionName))
                app.Xr.TryGetInstanceExtension(null, app.Instance, out _persistence);

            if (app.HasExtension(ExtSpatialPersistenceOperations.ExtensionName))
                app.Xr.TryGetInstanceExtension(null, app.Instance, out _persistenceOperations);

            _app.XrEvent += OnEvent;
        }

        public bool IsSupported()
        {
            return _app.HasExtension(ExtSpatialEntity.ExtensionName) && EnumerateCapabilities().Length > 0;
        }

        public unsafe SpatialCapabilityEXT[] EnumerateCapabilities()
        {
            uint count = 0;
            _app.CheckResult(_ext.EnumerateSpatialCapabilities(_app.Instance, _app.SystemId, 0, ref count, null), "EnumerateSpatialCapabilities");

            var result = new SpatialCapabilityEXT[count];
            fixed (SpatialCapabilityEXT* pResult = result)
                _app.CheckResult(_ext.EnumerateSpatialCapabilities(_app.Instance, _app.SystemId, count, ref count, pResult), "EnumerateSpatialCapabilities");

            return result;
        }

        public unsafe SpatialComponentTypeEXT[] EnumerateComponents(SpatialCapabilityEXT capability)
        {
            var info = new SpatialCapabilityComponentTypesEXT
            {
                Type = StructureType.SpatialCapabilityComponentTypesExt
            };
            _app.CheckResult(_ext.EnumerateSpatialCapabilityComponentTypes(_app.Instance, _app.SystemId, capability, ref info), "EnumerateSpatialCapabilityComponentTypes");

            var result = new SpatialComponentTypeEXT[info.ComponentTypeCountOutput];
            fixed (SpatialComponentTypeEXT* pResult = result)
            {
                info.ComponentTypeCapacityInput = (uint)result.Length;
                info.ComponentTypes = pResult;
                _app.CheckResult(_ext.EnumerateSpatialCapabilityComponentTypes(_app.Instance, _app.SystemId, capability, ref info), "EnumerateSpatialCapabilityComponentTypes");
            }

            return result;
        }

        public unsafe SpatialCapabilityFeatureEXT[] EnumerateFeatures(SpatialCapabilityEXT capability)
        {
            uint count = 0;
            _app.CheckResult(_ext.EnumerateSpatialCapabilityFeatures(_app.Instance, _app.SystemId, capability, 0, ref count, null), "EnumerateSpatialCapabilityFeatures");

            var result = new SpatialCapabilityFeatureEXT[count];
            fixed (SpatialCapabilityFeatureEXT* pResult = result)
                _app.CheckResult(_ext.EnumerateSpatialCapabilityFeatures(_app.Instance, _app.SystemId, capability, count, ref count, pResult), "EnumerateSpatialCapabilityFeatures");

            return result;
        }

        public async Task CreateAsync(params XrSpatialCapability[] capabilities)
        {
            EnsureIdle();

            if (IsCreated)
                throw new InvalidOperationException("Spatial context is already created");

            if (capabilities.Length == 0)
                throw new ArgumentException("At least one spatial capability is required", nameof(capabilities));

            _pendingOperations++;
            try
            {
                var future = CreateContext(capabilities);

                await _app.WaitFutureAsync(future);

                var completion = new CreateSpatialContextCompletionEXT
                {
                    Type = StructureType.CreateSpatialContextCompletionExt
                };

                _app.CheckResult(_ext.CreateSpatialContextComplete(_app.Session, future, ref completion), "CreateSpatialContextComplete");
                _app.CheckResult(completion.FutureResult, "CreateSpatialContext");

                _context = completion.SpatialContext;
            }
            finally
            {
                _pendingOperations--;
            }
        }

        protected unsafe FutureEXT CreateContext(XrSpatialCapability[] capabilities)
        {
            var allocations = new List<IDisposable>();
            try
            {
                var pointers = stackalloc SpatialCapabilityConfigurationBaseHeaderEXT*[capabilities.Length];
                for (var i = 0; i < capabilities.Length; i++)
                {
                    var capability = capabilities[i];
                    Type configType;
                    StructureType structureType;

                    switch (capability.Capability)
                    {
                        case SpatialCapabilityEXT.AnchorExt:
                            configType = typeof(SpatialCapabilityConfigurationAnchorEXT);
                            structureType = StructureType.SpatialCapabilityConfigurationAnchorExt;
                            break;
                        case SpatialCapabilityEXT.PlaneTrackingExt:
                            configType = typeof(SpatialCapabilityConfigurationPlaneTrackingEXT);
                            structureType = StructureType.SpatialCapabilityConfigurationPlaneTrackingExt;
                            break;
                        case SpatialCapabilityEXT.MarkerTrackingQRCodeExt:
                            configType = typeof(SpatialCapabilityConfigurationQrCodeEXT);
                            structureType = StructureType.SpatialCapabilityConfigurationQRCodeExt;
                            break;
                        case SpatialCapabilityEXT.MarkerTrackingMicroQRCodeExt:
                            configType = typeof(SpatialCapabilityConfigurationMicroQrCodeEXT);
                            structureType = StructureType.SpatialCapabilityConfigurationMicroQRCodeExt;
                            break;
                        case SpatialCapabilityEXT.MarkerTrackingArucoMarkerExt:
                            configType = typeof(SpatialCapabilityConfigurationArucoMarkerEXT);
                            structureType = StructureType.SpatialCapabilityConfigurationArucoMarkerExt;
                            break;
                        case SpatialCapabilityEXT.MarkerTrackingAprilTagExt:
                            configType = typeof(SpatialCapabilityConfigurationAprilTagEXT);
                            structureType = StructureType.SpatialCapabilityConfigurationAprilTagExt;
                            break;
                        default:
                            throw new NotSupportedException(capability.Capability.ToString());
                    }

                    var components = capability.Components ?? EnumerateComponents(capability.Capability);
                    var componentData = new NativeArray<SpatialComponentTypeEXT>(components.Length, typeof(SpatialComponentTypeEXT));
                    allocations.Add(componentData);
                    componentData.CopyFrom(components);

                    var config = new NativeArray<SpatialCapabilityConfigurationBaseHeaderEXT>(1, configType);
                    allocations.Add(config);
                    config[0] = new SpatialCapabilityConfigurationBaseHeaderEXT
                    {
                        Type = structureType,
                        Capability = capability.Capability,
                        EnabledComponentCount = (uint)components.Length,
                        EnabledComponents = componentData.Pointer
                    };

                    if (capability.Capability == SpatialCapabilityEXT.MarkerTrackingArucoMarkerExt)
                        config.ItemAt<SpatialCapabilityConfigurationArucoMarkerEXT>(0).ArUcoDict = capability.ArUcoDictionary;
                    else if (capability.Capability == SpatialCapabilityEXT.MarkerTrackingAprilTagExt)
                        config.ItemAt<SpatialCapabilityConfigurationAprilTagEXT>(0).AprilDict = capability.AprilTagDictionary;

                    if (capability.MarkerSideLength.HasValue)
                    {
                        var size = new NativeArray<SpatialMarkerSizeEXT>(1, typeof(SpatialMarkerSizeEXT));
                        allocations.Add(size);
                        size[0] = new SpatialMarkerSizeEXT
                        {
                            Type = StructureType.SpatialMarkerSizeExt,
                            Next = config[0].Next,
                            MarkerSideLength = capability.MarkerSideLength.Value
                        };
                        config[0].Next = size.Pointer;
                    }

                    if (capability.OptimizeForStaticMarker.HasValue)
                    {
                        var optimization = new NativeArray<SpatialMarkerStaticOptimizationEXT>(1, typeof(SpatialMarkerStaticOptimizationEXT));
                        allocations.Add(optimization);
                        optimization[0] = new SpatialMarkerStaticOptimizationEXT
                        {
                            Type = StructureType.SpatialMarkerStaticOptimizationExt,
                            Next = config[0].Next,
                            OptimizeForStaticMarker = capability.OptimizeForStaticMarker.Value ? 1u : 0u
                        };
                        config[0].Next = optimization.Pointer;
                    }

                    pointers[i] = config.Pointer;
                }

                var contexts = _persistenceContexts.Values.ToArray();
                fixed (SpatialPersistenceContextEXT* pContexts = contexts)
                {
                    var persistence = new SpatialContextPersistenceConfigEXT
                    {
                        Type = StructureType.SpatialContextPersistenceConfigExt,
                        PersistenceContextCount = (uint)contexts.Length,
                        PersistenceContexts = pContexts
                    };
                    var info = new SpatialContextCreateInfoEXT
                    {
                        Type = StructureType.SpatialContextCreateInfoExt,
                        CapabilityConfigCount = (uint)capabilities.Length,
                        CapabilityConfigs = pointers,
                        Next = contexts.Length == 0 ? null : &persistence
                    };

                    var future = new FutureEXT();
                    _app.CheckResult(_ext.CreateSpatialContextAsync(_app.Session, ref info, ref future), "CreateSpatialContextAsync");
                    return future;
                }
            }
            finally
            {
                foreach (var allocation in allocations)
                    allocation.Dispose();
            }
        }

        public XrSpatialEntity CreateAnchor(Pose3 pose, Space? baseSpace = null, long time = 0)
        {
            EnsureCreated();
            if (_anchor == null)
                throw new NotSupportedException(ExtSpatialAnchor.ExtensionName);

            var info = new SpatialAnchorCreateInfoEXT
            {
                Type = StructureType.SpatialAnchorCreateInfoExt,
                Pose = pose.ToPoseF(),
                BaseSpace = baseSpace ?? _app.ReferenceSpace,
                Time = time == 0 ? _app.FramePredictedDisplayTime : time
            };
            var result = new XrSpatialEntity();
            _app.CheckResult(_anchor.CreateSpatialAnchor(_context, ref info, ref result.Id, ref result.Handle), "CreateSpatialAnchor");
            return result;
        }

        public XrSpatialEntity CreateEntity(ulong entityId)
        {
            EnsureCreated();
            var info = new SpatialEntityFromIdCreateInfoEXT
            {
                Type = StructureType.SpatialEntityFromIDCreateInfoExt,
                EntityId = entityId
            };
            var result = new XrSpatialEntity { Id = entityId };
            _app.CheckResult(_ext.CreateSpatialEntityFromId(_context, ref info, ref result.Handle), "CreateSpatialEntityFromId");
            return result;
        }

        public void DestroyEntity(ref XrSpatialEntity entity)
        {
            EnsureCreated();
            if (entity.Handle.Handle == 0)
                return;

            _app.CheckResult(_ext.DestroySpatialEntity(entity.Handle), "DestroySpatialEntity");
            entity = default;
        }

        // Destroy the snapshot after reading its components and buffers.
        public async Task<SpatialSnapshotEXT> DiscoverAsync(
            SpatialComponentTypeEXT[]? components = null,
            Uuid[]? persistedUuids = null,
            Space? baseSpace = null,
            long time = 0,
            SpatialEntityTrackingStateEXT? trackingState = null)
        {
            EnsureCreated();
            _pendingOperations++;
            try
            {
                var future = CreateDiscoverySnapshot(components, persistedUuids, trackingState);
                await _app.WaitFutureAsync(future);

                var completionInfo = new CreateSpatialDiscoverySnapshotCompletionInfoEXT
                {
                    Type = StructureType.CreateSpatialDiscoverySnapshotCompletionInfoExt,
                    Future = future,
                    BaseSpace = baseSpace ?? _app.ReferenceSpace,
                    Time = time == 0 ? _app.FramePredictedDisplayTime : time
                };
                var completion = new CreateSpatialDiscoverySnapshotCompletionEXT
                {
                    Type = StructureType.CreateSpatialDiscoverySnapshotCompletionExt
                };
                _app.CheckResult(_ext.CreateSpatialDiscoverySnapshotComplete(_context, ref completionInfo, ref completion), "CreateSpatialDiscoverySnapshotComplete");
                _app.CheckResult(completion.FutureResult, "CreateSpatialDiscoverySnapshot");
                return completion.Snapshot;
            }
            finally
            {
                _pendingOperations--;
            }
        }

        protected unsafe FutureEXT CreateDiscoverySnapshot(
            SpatialComponentTypeEXT[]? components,
            Uuid[]? persistedUuids,
            SpatialEntityTrackingStateEXT? trackingState)
        {
            fixed (SpatialComponentTypeEXT* pComponents = components)
            fixed (Uuid* pUuids = persistedUuids)
            {
                var stateFilter = new SpatialFilterTrackingStateEXT
                {
                    Type = StructureType.SpatialFilterTrackingStateExt,
                    TrackingState = trackingState.GetValueOrDefault()
                };
                var uuidFilter = new SpatialDiscoveryPersistenceUuidFilterEXT
                {
                    Type = StructureType.SpatialDiscoveryPersistenceUuidFilterExt,
                    PersistedUuidCount = (uint)(persistedUuids?.Length ?? 0),
                    PersistedUuids = pUuids,
                    Next = trackingState.HasValue ? &stateFilter : null
                };
                var info = new SpatialDiscoverySnapshotCreateInfoEXT
                {
                    Type = StructureType.SpatialDiscoverySnapshotCreateInfoExt,
                    ComponentTypeCount = (uint)(components?.Length ?? 0),
                    ComponentTypes = pComponents,
                    Next = uuidFilter.PersistedUuidCount == 0 ? uuidFilter.Next : &uuidFilter
                };
                var future = new FutureEXT();
                _app.CheckResult(_ext.CreateSpatialDiscoverySnapshotAsync(_context, ref info, ref future), "CreateSpatialDiscoverySnapshotAsync");
                return future;
            }
        }

        public Pose3? LocateAnchor(XrSpatialEntity anchor, Space? baseSpace = null, long time = 0)
        {
            var snapshot = CreateUpdateSnapshot([anchor], [SpatialComponentTypeEXT.AnchorExt], baseSpace, time);
            try
            {
                var locations = GetAnchors(snapshot);
                for (var i = 0; i < locations.EntityIds.Length; i++)
                {
                    if (locations.EntityIds[i] == anchor.Id && locations.EntityStates[i] == SpatialEntityTrackingStateEXT.TrackingExt)
                        return locations.Data[i].ToPose3();
                }
                return null;
            }
            finally
            {
                DestroySnapshot(ref snapshot);
            }
        }

        // The returned snapshot must be destroyed after reading its data.
        public unsafe SpatialSnapshotEXT CreateUpdateSnapshot(
            XrSpatialEntity[] entities,
            SpatialComponentTypeEXT[]? components = null,
            Space? baseSpace = null,
            long time = 0)
        {
            EnsureCreated();
            var handles = new SpatialEntityEXT[entities.Length];
            for (var i = 0; i < entities.Length; i++)
                handles[i] = entities[i].Handle;

            fixed (SpatialEntityEXT* pEntities = handles)
            fixed (SpatialComponentTypeEXT* pComponents = components)
            {
                var info = new SpatialUpdateSnapshotCreateInfoEXT
                {
                    Type = StructureType.SpatialUpdateSnapshotCreateInfoExt,
                    EntityCount = (uint)handles.Length,
                    Entities = pEntities,
                    ComponentTypeCount = (uint)(components?.Length ?? 0),
                    ComponentTypes = pComponents,
                    BaseSpace = baseSpace ?? _app.ReferenceSpace,
                    Time = time == 0 ? _app.FramePredictedDisplayTime : time
                };
                var result = new SpatialSnapshotEXT();
                _app.CheckResult(_ext.CreateSpatialUpdateSnapshot(_context, ref info, ref result), "CreateSpatialUpdateSnapshot");
                return result;
            }
        }

        public void DestroySnapshot(ref SpatialSnapshotEXT snapshot)
        {
            EnsureCreated();
            if (snapshot.Handle == 0)
                return;

            _app.CheckResult(_ext.DestroySpatialSnapshot(snapshot), "DestroySpatialSnapshot");
            snapshot.Handle = 0;
        }

        public unsafe SpatialPersistenceScopeEXT[] EnumeratePersistenceScopes()
        {
            if (_persistence == null)
                throw new NotSupportedException(ExtSpatialPersistence.ExtensionName);

            uint count = 0;
            _app.CheckResult(_persistence.EnumerateSpatialPersistenceScopes(_app.Instance, _app.SystemId, 0, ref count, null), "EnumerateSpatialPersistenceScopes");
            var result = new SpatialPersistenceScopeEXT[count];
            fixed (SpatialPersistenceScopeEXT* pResult = result)
                _app.CheckResult(_persistence.EnumerateSpatialPersistenceScopes(_app.Instance, _app.SystemId, count, ref count, pResult), "EnumerateSpatialPersistenceScopes");

            return result;
        }

        // Create persistence before the spatial context so CreateAsync can connect them.
        public async Task CreatePersistenceAsync(SpatialPersistenceScopeEXT scope)
        {
            EnsureIdle();
            if (_persistence == null)
                throw new NotSupportedException(ExtSpatialPersistence.ExtensionName);
            if (IsCreated || _persistenceContexts.ContainsKey(scope))
                throw new InvalidOperationException("Create persistence before creating the spatial context");

            _pendingOperations++;
            try
            {
                var info = new SpatialPersistenceContextCreateInfoEXT
                {
                    Type = StructureType.SpatialPersistenceContextCreateInfoExt,
                    Scope = scope
                };
                var future = new FutureEXT();
                _app.CheckResult(_persistence.CreateSpatialPersistenceContextAsync(_app.Session, ref info, ref future), "CreateSpatialPersistenceContextAsync");
                await _app.WaitFutureAsync(future);

                var completion = new CreateSpatialPersistenceContextCompletionEXT
                {
                    Type = StructureType.CreateSpatialPersistenceContextCompletionExt
                };
                _app.CheckResult(_persistence.CreateSpatialPersistenceContextComplete(_app.Session, future, ref completion), "CreateSpatialPersistenceContextComplete");
                _app.CheckResult(completion.FutureResult, "CreateSpatialPersistenceContext");
                CheckPersistenceResult(completion.CreateResult, "CreateSpatialPersistenceContext");
                _persistenceContexts.Add(scope, completion.PersistenceContext);
            }
            finally
            {
                _pendingOperations--;
            }
        }

        public async Task<Uuid> PersistAsync(ulong entityId, SpatialPersistenceScopeEXT scope = SpatialPersistenceScopeEXT.LocalAnchorsExt)
        {
            EnsureCreated();
            var persistenceContext = GetPersistenceContext(scope);
            _pendingOperations++;
            try
            {
                var info = new SpatialEntityPersistInfoEXT
                {
                    Type = StructureType.SpatialEntityPersistInfoExt,
                    SpatialContext = _context,
                    SpatialEntityId = entityId
                };
                var future = new FutureEXT();
                _app.CheckResult(_persistenceOperations!.PersistSpatialEntityAsync(persistenceContext, ref info, ref future), "PersistSpatialEntityAsync");
                await _app.WaitFutureAsync(future);

                var completion = new PersistSpatialEntityCompletionEXT
                {
                    Type = StructureType.PersistSpatialEntityCompletionExt
                };
                _app.CheckResult(_persistenceOperations.PersistSpatialEntityComplete(persistenceContext, future, ref completion), "PersistSpatialEntityComplete");
                _app.CheckResult(completion.FutureResult, "PersistSpatialEntity");
                CheckPersistenceResult(completion.PersistResult, "PersistSpatialEntity");
                return completion.PersistUuid;
            }
            finally
            {
                _pendingOperations--;
            }
        }

        public async Task UnpersistAsync(Uuid uuid, SpatialPersistenceScopeEXT scope = SpatialPersistenceScopeEXT.LocalAnchorsExt)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(XrSpatial));
            var persistenceContext = GetPersistenceContext(scope);
            _pendingOperations++;
            try
            {
                var info = new SpatialEntityUnpersistInfoEXT
                {
                    Type = StructureType.SpatialEntityUnpersistInfoExt,
                    PersistUuid = uuid
                };
                var future = new FutureEXT();
                _app.CheckResult(_persistenceOperations!.UnpersistSpatialEntityAsync(persistenceContext, ref info, ref future), "UnpersistSpatialEntityAsync");
                await _app.WaitFutureAsync(future);

                var completion = new UnpersistSpatialEntityCompletionEXT
                {
                    Type = StructureType.UnpersistSpatialEntityCompletionExt
                };
                _app.CheckResult(_persistenceOperations.UnpersistSpatialEntityComplete(persistenceContext, future, ref completion), "UnpersistSpatialEntityComplete");
                _app.CheckResult(completion.FutureResult, "UnpersistSpatialEntity");
                CheckPersistenceResult(completion.UnpersistResult, "UnpersistSpatialEntity");
            }
            finally
            {
                _pendingOperations--;
            }
        }

        protected void CheckPersistenceResult(SpatialPersistenceContextResultEXT result, string operation)
        {
            if (result != SpatialPersistenceContextResultEXT.SuccessExt)
                throw new InvalidOperationException($"{operation}: {result}");
        }

        protected SpatialPersistenceContextEXT GetPersistenceContext(SpatialPersistenceScopeEXT scope)
        {
            if (_persistenceOperations == null)
                throw new NotSupportedException(ExtSpatialPersistenceOperations.ExtensionName);
            if (!_persistenceContexts.TryGetValue(scope, out var context))
                throw new InvalidOperationException($"Spatial persistence context is not created: {scope}");
            return context;
        }

        public bool HasPersistenceScope(SpatialPersistenceScopeEXT scope)
        {
            return _persistenceContexts.ContainsKey(scope);
        }

        protected void EnsureCreated()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(XrSpatial));

            if (!IsCreated)
                throw new InvalidOperationException("Spatial context is not created");
        }

        protected void EnsureIdle()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(XrSpatial));

            if (_pendingOperations != 0)
                throw new InvalidOperationException("Spatial operations are still pending");
        }

        protected void OnEvent(ref EventDataBuffer buffer)
        {
            if (buffer.Type != StructureType.EventDataSpatialDiscoveryRecommendedExt)
                return;

            var data = buffer.Convert().To<EventDataSpatialDiscoveryRecommendedEXT>();
            if (IsCreated && data.SpatialContext.Handle == _context.Handle)
                DiscoveryRecommended?.Invoke();
        }

        public void Destroy()
        {
            EnsureIdle();
            if (_context.Handle != 0)
            {
                // Destroying the context also invalidates its entities and snapshots.
                _app.CheckResult(_ext.DestroySpatialContext(_context), "DestroySpatialContext");
                _context.Handle = 0;
            }
        }

        public void DestroyPersistence()
        {
            EnsureIdle();
           
            if (IsCreated)
                throw new InvalidOperationException("Destroy the spatial context before persistence");

            foreach (var item in _persistenceContexts.ToArray())
            {
                _app.CheckResult(_persistence!.DestroySpatialPersistenceContext(item.Value), "DestroySpatialPersistenceContext");
                _persistenceContexts.Remove(item.Key);
            }
        }
        public void Dispose()
        {
            if (_disposed)
                return;

            if (_app.Session.Handle == _session.Handle && _session.Handle != 0)
            {
                Destroy();
                DestroyPersistence();
            }
            else
            {
                _context = default;
                _persistenceContexts.Clear();
            }
            _app.XrEvent -= OnEvent;
            _anchor?.Dispose();
            _persistenceOperations?.Dispose();
            _persistence?.Dispose();
            _ext.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }


        public unsafe XrSpatialQueryResult QueryEntities(
            SpatialSnapshotEXT snapshot,
            SpatialComponentTypeEXT[] components,
            SpatialEntityTrackingStateEXT? trackingState = null)
        {
            EnsureCreated();
            fixed (SpatialComponentTypeEXT* pComponents = components)
            {
                var filter = new SpatialFilterTrackingStateEXT
                {
                    Type = StructureType.SpatialFilterTrackingStateExt,
                    TrackingState = trackingState.GetValueOrDefault()
                };
                var condition = new SpatialComponentDataQueryConditionEXT
                {
                    Type = StructureType.SpatialComponentDataQueryConditionExt,
                    ComponentTypeCount = (uint)components.Length,
                    ComponentTypes = pComponents,
                    Next = trackingState.HasValue ? &filter : null
                };
                var result = new SpatialComponentDataQueryResultEXT
                {
                    Type = StructureType.SpatialComponentDataQueryResultExt
                };
                _app.CheckResult(_ext.QuerySpatialComponentData(snapshot, ref condition, ref result), "QuerySpatialComponentData");

                var data = new XrSpatialQueryResult
                {
                    EntityIds = new ulong[result.EntityIdCountOutput],
                    EntityStates = new SpatialEntityTrackingStateEXT[result.EntityStateCountOutput]
                };
                fixed (ulong* pIds = data.EntityIds)
                fixed (SpatialEntityTrackingStateEXT* pStates = data.EntityStates)
                {
                    result.EntityIdCapacityInput = (uint)data.EntityIds.Length;
                    result.EntityIds = pIds;
                    result.EntityStateCapacityInput = (uint)data.EntityStates.Length;
                    result.EntityStates = pStates;
                    _app.CheckResult(_ext.QuerySpatialComponentData(snapshot, ref condition, ref result), "QuerySpatialComponentData");
                }
                return data;
            }
        }

        protected unsafe void ReadComponent(
            SpatialSnapshotEXT snapshot,
            SpatialComponentTypeEXT component,
            XrSpatialQueryResult entities,
            void* componentList)
        {
            if (entities.EntityIds.Length == 0)
                return;

            var condition = new SpatialComponentDataQueryConditionEXT
            {
                Type = StructureType.SpatialComponentDataQueryConditionExt,
                ComponentTypeCount = 1,
                ComponentTypes = &component
            };
            fixed (ulong* pIds = entities.EntityIds)
            fixed (SpatialEntityTrackingStateEXT* pStates = entities.EntityStates)
            {
                var result = new SpatialComponentDataQueryResultEXT
                {
                    Type = StructureType.SpatialComponentDataQueryResultExt,
                    Next = componentList,
                    EntityIdCapacityInput = (uint)entities.EntityIds.Length,
                    EntityIds = pIds,
                    EntityStateCapacityInput = (uint)entities.EntityStates.Length,
                    EntityStates = pStates
                };
                _app.CheckResult(_ext.QuerySpatialComponentData(snapshot, ref condition, ref result), "QuerySpatialComponentData");
            }
        }

        public unsafe XrSpatialComponentData<Posef> GetAnchors(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.AnchorExt]);
            var data = new Posef[entities.EntityIds.Length];
            fixed (Posef* pData = data)
            {
                var list = new SpatialComponentAnchorListEXT
                {
                    Type = StructureType.SpatialComponentAnchorListExt,
                    LocationCount = (uint)data.Length,
                    Locations = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.AnchorExt, entities, &list);
            }
            return new XrSpatialComponentData<Posef>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<SpatialBounded2DDataEXT> GetBounds2D(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.Bounded2DExt]);
            var data = new SpatialBounded2DDataEXT[entities.EntityIds.Length];
            fixed (SpatialBounded2DDataEXT* pData = data)
            {
                var list = new SpatialComponentBounded2DListEXT
                {
                    Type = StructureType.SpatialComponentBounded2DListExt,
                    BoundCount = (uint)data.Length,
                    Bounds = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.Bounded2DExt, entities, &list);
            }
            return new XrSpatialComponentData<SpatialBounded2DDataEXT>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<Boxf> GetBounds3D(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.Bounded3DExt]);
            var data = new Boxf[entities.EntityIds.Length];
            fixed (Boxf* pData = data)
            {
                var list = new SpatialComponentBounded3DListEXT
                {
                    Type = StructureType.SpatialComponentBounded3DListExt,
                    BoundCount = (uint)data.Length,
                    Bounds = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.Bounded3DExt, entities, &list);
            }
            return new XrSpatialComponentData<Boxf>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<ulong> GetParents(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.ParentExt]);
            var data = new ulong[entities.EntityIds.Length];
            fixed (ulong* pData = data)
            {
                var list = new SpatialComponentParentListEXT
                {
                    Type = StructureType.SpatialComponentParentListExt,
                    ParentCount = (uint)data.Length,
                    Parents = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.ParentExt, entities, &list);
            }
            return new XrSpatialComponentData<ulong>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        // Mesh, polygon and marker buffer IDs remain valid only while their snapshot is alive.
        public unsafe XrSpatialComponentData<SpatialMeshDataEXT> GetMeshes3D(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.Mesh3DExt]);
            var data = new SpatialMeshDataEXT[entities.EntityIds.Length];
            fixed (SpatialMeshDataEXT* pData = data)
            {
                var list = new SpatialComponentMesh3DListEXT
                {
                    Type = StructureType.SpatialComponentMesh3DListExt,
                    MeshCount = (uint)data.Length,
                    Meshes = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.Mesh3DExt, entities, &list);
            }
            return new XrSpatialComponentData<SpatialMeshDataEXT>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<SpatialMeshDataEXT> GetMeshes2D(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.Mesh2DExt]);
            var data = new SpatialMeshDataEXT[entities.EntityIds.Length];
            fixed (SpatialMeshDataEXT* pData = data)
            {
                var list = new SpatialComponentMesh2DListEXT
                {
                    Type = StructureType.SpatialComponentMesh2DListExt,
                    MeshCount = (uint)data.Length,
                    Meshes = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.Mesh2DExt, entities, &list);
            }
            return new XrSpatialComponentData<SpatialMeshDataEXT>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<SpatialPolygon2DDataEXT> GetPolygons2D(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.Polygon2DExt]);
            var data = new SpatialPolygon2DDataEXT[entities.EntityIds.Length];
            fixed (SpatialPolygon2DDataEXT* pData = data)
            {
                var list = new SpatialComponentPolygon2DListEXT
                {
                    Type = StructureType.SpatialComponentPolygon2DListExt,
                    PolygonCount = (uint)data.Length,
                    Polygons = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.Polygon2DExt, entities, &list);
            }
            return new XrSpatialComponentData<SpatialPolygon2DDataEXT>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<SpatialPlaneAlignmentEXT> GetPlaneAlignments(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.PlaneAlignmentExt]);
            var data = new SpatialPlaneAlignmentEXT[entities.EntityIds.Length];
            fixed (SpatialPlaneAlignmentEXT* pData = data)
            {
                var list = new SpatialComponentPlaneAlignmentListEXT
                {
                    Type = StructureType.SpatialComponentPlaneAlignmentListExt,
                    PlaneAlignmentCount = (uint)data.Length,
                    PlaneAlignments = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.PlaneAlignmentExt, entities, &list);
            }
            return new XrSpatialComponentData<SpatialPlaneAlignmentEXT>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<SpatialPlaneSemanticLabelEXT> GetPlaneSemanticLabels(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.PlaneSemanticLabelExt]);
            var data = new SpatialPlaneSemanticLabelEXT[entities.EntityIds.Length];
            fixed (SpatialPlaneSemanticLabelEXT* pData = data)
            {
                var list = new SpatialComponentPlaneSemanticLabelListEXT
                {
                    Type = StructureType.SpatialComponentPlaneSemanticLabelListExt,
                    SemanticLabelCount = (uint)data.Length,
                    SemanticLabels = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.PlaneSemanticLabelExt, entities, &list);
            }
            return new XrSpatialComponentData<SpatialPlaneSemanticLabelEXT>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<SpatialMarkerDataEXT> GetMarkers(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.MarkerExt]);
            var data = new SpatialMarkerDataEXT[entities.EntityIds.Length];
            fixed (SpatialMarkerDataEXT* pData = data)
            {
                var list = new SpatialComponentMarkerListEXT
                {
                    Type = StructureType.SpatialComponentMarkerListExt,
                    MarkerCount = (uint)data.Length,
                    Markers = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.MarkerExt, entities, &list);
            }
            return new XrSpatialComponentData<SpatialMarkerDataEXT>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe XrSpatialComponentData<SpatialPersistenceDataEXT> GetPersistenceData(SpatialSnapshotEXT snapshot)
        {
            var entities = QueryEntities(snapshot, [SpatialComponentTypeEXT.PersistenceExt]);
            var data = new SpatialPersistenceDataEXT[entities.EntityIds.Length];
            fixed (SpatialPersistenceDataEXT* pData = data)
            {
                var list = new SpatialComponentPersistenceListEXT
                {
                    Type = StructureType.SpatialComponentPersistenceListExt,
                    PersistDataCount = (uint)data.Length,
                    PersistData = pData
                };
                ReadComponent(snapshot, SpatialComponentTypeEXT.PersistenceExt, entities, &list);
            }
            return new XrSpatialComponentData<SpatialPersistenceDataEXT>
            {
                EntityIds = entities.EntityIds,
                EntityStates = entities.EntityStates,
                Data = data
            };
        }

        public unsafe byte[] GetBufferBytes(SpatialSnapshotEXT snapshot, ulong bufferId)
        {
            EnsureCreated();
            var info = new SpatialBufferGetInfoEXT
            {
                Type = StructureType.SpatialBufferGetInfoExt,
                BufferId = bufferId
            };
            uint count = 0;
            _app.CheckResult(_ext.GetSpatialBufferUint8(snapshot, ref info, 0, ref count, (byte*)null), "GetSpatialBufferUint8");
            var result = new byte[count];
            fixed (byte* pResult = result)
                _app.CheckResult(_ext.GetSpatialBufferUint8(snapshot, ref info, count, ref count, pResult), "GetSpatialBufferUint8");

            return result;
        }

        public unsafe ushort[] GetBufferUInt16(SpatialSnapshotEXT snapshot, ulong bufferId)
        {
            EnsureCreated();
            var info = new SpatialBufferGetInfoEXT
            {
                Type = StructureType.SpatialBufferGetInfoExt,
                BufferId = bufferId
            };
            uint count = 0;
            _app.CheckResult(_ext.GetSpatialBufferUint16(snapshot, ref info, 0, ref count, null), "GetSpatialBufferUint16");
            var result = new ushort[count];
            fixed (ushort* pResult = result)
                _app.CheckResult(_ext.GetSpatialBufferUint16(snapshot, ref info, count, ref count, pResult), "GetSpatialBufferUint16");

            return result;
        }

        public unsafe uint[] GetBufferUInt32(SpatialSnapshotEXT snapshot, ulong bufferId)
        {
            EnsureCreated();
            var info = new SpatialBufferGetInfoEXT
            {
                Type = StructureType.SpatialBufferGetInfoExt,
                BufferId = bufferId
            };
            uint count = 0;
            _app.CheckResult(_ext.GetSpatialBufferUint32(snapshot, ref info, 0, ref count, null), "GetSpatialBufferUint32");
            var result = new uint[count];
            fixed (uint* pResult = result)
                _app.CheckResult(_ext.GetSpatialBufferUint32(snapshot, ref info, count, ref count, pResult), "GetSpatialBufferUint32");

            return result;
        }

        public unsafe float[] GetBufferFloats(SpatialSnapshotEXT snapshot, ulong bufferId)
        {
            EnsureCreated();
            var info = new SpatialBufferGetInfoEXT
            {
                Type = StructureType.SpatialBufferGetInfoExt,
                BufferId = bufferId
            };
            uint count = 0;
            _app.CheckResult(_ext.GetSpatialBufferFloat(snapshot, ref info, 0, ref count, null), "GetSpatialBufferFloat");
            var result = new float[count];
            fixed (float* pResult = result)
                _app.CheckResult(_ext.GetSpatialBufferFloat(snapshot, ref info, count, ref count, pResult), "GetSpatialBufferFloat");

            return result;
        }

        public unsafe Vector2f[] GetBufferVector2(SpatialSnapshotEXT snapshot, ulong bufferId)
        {
            EnsureCreated();
            var info = new SpatialBufferGetInfoEXT
            {
                Type = StructureType.SpatialBufferGetInfoExt,
                BufferId = bufferId
            };
            uint count = 0;
            _app.CheckResult(_ext.GetSpatialBufferVector2(snapshot, ref info, 0, ref count, null), "GetSpatialBufferVector2");
            var result = new Vector2f[count];
            fixed (Vector2f* pResult = result)
                _app.CheckResult(_ext.GetSpatialBufferVector2(snapshot, ref info, count, ref count, pResult), "GetSpatialBufferVector2");

            return result;
        }

        public unsafe Vector3f[] GetBufferVector3(SpatialSnapshotEXT snapshot, ulong bufferId)
        {
            EnsureCreated();
            var info = new SpatialBufferGetInfoEXT
            {
                Type = StructureType.SpatialBufferGetInfoExt,
                BufferId = bufferId
            };
            uint count = 0;
            _app.CheckResult(_ext.GetSpatialBufferVector3(snapshot, ref info, 0, ref count, null), "GetSpatialBufferVector3");
            var result = new Vector3f[count];
            fixed (Vector3f* pResult = result)
                _app.CheckResult(_ext.GetSpatialBufferVector3(snapshot, ref info, count, ref count, pResult), "GetSpatialBufferVector3");

            return result;
        }

        public unsafe string GetBufferString(SpatialSnapshotEXT snapshot, ulong bufferId)
        {
            EnsureCreated();
            var info = new SpatialBufferGetInfoEXT
            {
                Type = StructureType.SpatialBufferGetInfoExt,
                BufferId = bufferId
            };
            uint count = 0;
            _app.CheckResult(_ext.GetSpatialBufferString(snapshot, ref info, 0, ref count, (byte*)null), "GetSpatialBufferString");
            var result = new byte[count];
            fixed (byte* pResult = result)
                _app.CheckResult(_ext.GetSpatialBufferString(snapshot, ref info, count, ref count, pResult), "GetSpatialBufferString");

            return Encoding.UTF8.GetString(result).TrimEnd('\0');
        }

        public bool IsCreated => !_disposed && _context.Handle != 0 && _app.Session.Handle == _session.Handle;

        public bool IsPersistenceCreated => _persistenceContexts.Count > 0;

        public event System.Action? DiscoveryRecommended;

        public static implicit operator SpatialContextEXT(XrSpatial value)
        {
            return value._context;
        }
    }
}
