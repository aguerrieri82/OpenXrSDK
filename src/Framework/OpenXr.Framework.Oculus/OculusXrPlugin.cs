using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using XrMath;
using Action = Silk.NET.OpenXR.Action;


namespace OpenXr.Framework.Oculus
{

    public class OculusXrPluginOptions
    {
        public SwapchainCreateFoveationFlagsFB Foveation { get; set; }

        public static readonly OculusXrPluginOptions Default = new()
        {
            Foveation = SwapchainCreateFoveationFlagsFB.ScaledBinBitFB
        };
    }

    public class OculusXrPlugin : XrBasePlugin, IDisposable
    {
        public static readonly string[] LABELS = ["CEILING", "DOOR_FRAME", "FLOOR", "INVISIBLE_WALL_FACE", "WALL_ART", "WALL_FACE", "WINDOW_FRAME", "COUCH", "TABLE", "BED", "LAMP", "PLANT", "SCREEN", "STORAGE", "GLOBAL_MESH", "OTHER"];

        #region EXTENSIONS

        public const SpaceComponentTypeFB XR_SPACE_COMPONENT_TYPE_TRIANGLE_MESH_META = (SpaceComponentTypeFB)1000269000;

        const StructureType XR_TYPE_SPACE_TRIANGLE_MESH_GET_INFO_META = (StructureType)1000269001;

        const StructureType XR_TYPE_SPACE_TRIANGLE_MESH_META = (StructureType)1000269002;

        [StructLayout(LayoutKind.Sequential)]
        struct SpaceTriangleMeshGetInfoMETA
        {
            public StructureType Type;
            public unsafe void* Next;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct SpaceTriangleMeshMETA
        {
            public StructureType Type;
            public unsafe void* Next;
            public uint VertexCapacityInput;
            public uint VertexCountOutput;
            public unsafe Vector3f* Vertices;
            public uint IndexCapacityInput;
            public uint IndexCountOutput;
            public unsafe uint* Indices;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate Result GetSpaceTriangleMeshMETADelegate(Space space, ref SpaceTriangleMeshGetInfoMETA getInfo, ref SpaceTriangleMeshMETA triangleMeshOutput);

        GetSpaceTriangleMeshMETADelegate? GetSpaceTriangleMeshMETA;

        #endregion

        #region DEPTH



        #endregion

        protected class ActiveQuery
        {
            public string? Hash { get; set; }

            public object? Completion { get; set; }

            public ulong ReqId { get; set; }
        }

        protected FBScene? _scene;
        protected FBSpatialEntity? _spatial;
        protected FBSpatialEntityQuery? _spatialQuery;
        protected FBTriangleMesh? _mesh;
        protected NativeStruct<SwapchainCreateInfoFoveationFB> _foveationInfo;
        protected FBHapticPcm? _haptic;
        protected FBDisplayRefreshRate? _refreshRate;
        protected FBSpatialEntityContainer? _container;
        protected FBFoveation? _foveation;
        protected FBSwapchainUpdateState? _swapChainUpdate;
        protected FBHandTrackingMesh? _handMesh;
        protected FBSpatialEntityStorage? _spatialStorage;


        protected readonly Dictionary<string, ActiveQuery> _queries = [];


        protected readonly OculusXrPluginOptions _options;

        public OculusXrPlugin()
            : this(OculusXrPluginOptions.Default)
        {
        }

        public OculusXrPlugin(OculusXrPluginOptions options)
        {
            _options = options;
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            _app = app;

            extensions.Add(FBScene.ExtensionName);
            extensions.Add(FBSceneCapture.ExtensionName);
            extensions.Add(FBTriangleMesh.ExtensionName);
            extensions.Add(FBSpatialEntity.ExtensionName);
            extensions.Add(FBSpatialEntityContainer.ExtensionName);
            extensions.Add(FBSpatialEntityStorage.ExtensionName);
            extensions.Add(FBSpatialEntityQuery.ExtensionName);
            extensions.Add(FBHapticPcm.ExtensionName);
            extensions.Add(FBDisplayRefreshRate.ExtensionName);
            extensions.Add(FBFoveation.ExtensionName);
            extensions.Add(FBSwapchainUpdateState.ExtensionName);
            extensions.Add(FBHandTrackingMesh.ExtensionName);
            extensions.Add(METAEnvironmentDepth.ExtensionName);

            extensions.Add("XR_FB_hand_tracking_capsules");
            extensions.Add("XR_FB_hand_tracking_aim");
            extensions.Add("XR_META_spatial_entity_mesh");
            extensions.Add("XR_META_touch_controller_plus");
            extensions.Add("XR_FB_foveation_configuration");

        }

        public unsafe override void OnInstanceCreated()
        {
            Debug.Assert(_app != null);

            _app.Xr.TryGetInstanceExtension<FBScene>(null, _app.Instance, out _scene);
            _app.Xr.TryGetInstanceExtension<FBSpatialEntity>(null, _app.Instance, out _spatial);
            _app.Xr.TryGetInstanceExtension<FBSpatialEntityQuery>(null, _app.Instance, out _spatialQuery);
            _app.Xr.TryGetInstanceExtension<FBTriangleMesh>(null, _app.Instance, out _mesh);
            _app.Xr.TryGetInstanceExtension<FBHapticPcm>(null, _app.Instance, out _haptic);
            _app.Xr.TryGetInstanceExtension<FBSpatialEntityContainer>(null, _app.Instance, out _container);
            _app.Xr.TryGetInstanceExtension<FBDisplayRefreshRate>(null, _app.Instance, out _refreshRate);
            _app.Xr.TryGetInstanceExtension<FBFoveation>(null, _app.Instance, out _foveation);
            _app.Xr.TryGetInstanceExtension<FBSwapchainUpdateState>(null, _app.Instance, out _swapChainUpdate);
            _app.Xr.TryGetInstanceExtension<FBHandTrackingMesh>(null, _app.Instance, out _handMesh);
            _app.Xr.TryGetInstanceExtension<FBSpatialEntityStorage>(null, _app.Instance, out _spatialStorage);

            var func = new PfnVoidFunction();
            _app.CheckResult(_app.Xr.GetInstanceProcAddr(_app.Instance, "xrGetSpaceTriangleMeshMETA", &func), "Bind xrGetSpaceTriangleMeshMETA");
            GetSpaceTriangleMeshMETA = Marshal.GetDelegateForFunctionPointer<GetSpaceTriangleMeshMETADelegate>(new nint(func.Handle));
        }

        public unsafe string[] GetSpaceSemanticLabels(Space space)
        {
            var labels = string.Join(',', LABELS);

            var support = new SemanticLabelsSupportInfoFB
            {
                Type = StructureType.SemanticLabelsSupportInfoFB,
                Flags = SemanticLabelsSupportFlagsFB.MultipleSemanticLabelsBitFB,
                RecognizedLabels = (byte*)SilkMarshal.StringToPtr(labels)
            };

            var result = new SemanticLabelsFB
            {
                Type = StructureType.SemanticLabelsFB,
                Next = &support
            };

            _app!.CheckResult(_scene!.GetSpaceSemanticLabelsFB(_app!.Session, space, ref result), "GetSpaceSemanticLabelsFB");
            var buffer = new byte[result.BufferCountOutput];
            fixed (byte* pBuffer = buffer)
            {
                result.Buffer = pBuffer;
                result.BufferCapacityInput = result.BufferCountOutput;
            }

            _app!.CheckResult(_scene!.GetSpaceSemanticLabelsFB(_app!.Session, space, ref result), "GetSpaceSemanticLabelsFB");

            return Encoding.UTF8.GetString(buffer).Trim('\0').Split(',');
        }

        public unsafe Task<Space> CreateAnchorAsync(Pose3 pose, Space refSpace)
        {
            var info = new SpatialAnchorCreateInfoFB()
            {
                Type = StructureType.SpatialAnchorCreateInfoFB,
                PoseInSpace = pose.ToPoseF(),
                Space = refSpace
            };

            ulong reqId = 1;
            _app!.CheckResult(_spatial!.CreateSpatialAnchorFB(_app.Session, &info, ref reqId), "CreateSpatialAnchorFB");

            throw new NotImplementedException();
        }

        public async Task SaveSpaceAsync(Space space, bool saveLocal)
        {
            if (!GetSpaceComponentEnabled(space, SpaceComponentTypeFB.StorableFB))
                await SetSpaceComponentStatusAsync(space, SpaceComponentTypeFB.StorableFB, true);

            var reqId = SaveSpaceRequest(space, saveLocal);

            throw new NotImplementedException();

        }

        public unsafe ulong SaveSpaceRequest(Space space, bool saveLocal)
        {
            var info = new SpaceSaveInfoFB()
            {
                Type = StructureType.SpaceSaveInfoFB,
                Space = space,
                Location = saveLocal ? SpaceStorageLocationFB.LocalFB : SpaceStorageLocationFB.CloudFB,
                PersistenceMode = SpacePersistenceModeFB.IndefiniteFB
            };

            ulong reqId = 0;

            _app!.CheckResult(_spatialStorage!.SaveSpaceFB(_app.Session, &info, ref reqId), "SaveSpaceFB");

            return reqId;
        }

        public Task EraseSpaceAsync(Space space, bool isLocal)
        {
            throw new NotImplementedException();
        }

        public unsafe ulong EraseSpaceRequest(Space space, bool isLocal)
        {
            var info = new SpaceEraseInfoFB()
            {
                Type = StructureType.SpaceEraseInfoFB,
                Space = space,
                Location = isLocal ? SpaceStorageLocationFB.LocalFB : SpaceStorageLocationFB.CloudFB,
            };

            ulong reqId = 0;

            _app!.CheckResult(_spatialStorage!.EraseSpaceFB(_app.Session, &info, ref reqId), "EraseSpaceFB");

            return reqId;
        }

        public Rect2Df GetSpaceBoundingBox2D(Space space)
        {
            var result = new Rect2Df();
            _app!.CheckResult(_scene!.GetSpaceBoundingBox2Dfb(_app!.Session, space, ref result), "GetSpaceBoundingBox2D");
            return result;
        }

        public Rect3DfFB GetSpaceBoundingBox3D(Space space)
        {
            var result = new Rect3DfFB();
            _app!.CheckResult(_scene!.GetSpaceBoundingBox3Dfb(_app!.Session, space, ref result), "GetSpaceBoundingBox2D");
            return result;
        }

        public bool GetSpaceComponentEnabled(Space space, SpaceComponentTypeFB componentType)
        {
            var status = new SpaceComponentStatusFB()
            {
                Type = StructureType.SpaceComponentStatusFB
            };

            var result = _spatial!.GetSpaceComponentStatusFB(space, componentType, ref status);
            if (result == Result.ErrorSpaceComponentNotSupportedFB)
                return false;

            _app!.CheckResult(result, "GetSpaceComponentStatus");

            return status.Enabled != 0;
        }

        public ulong SetSpaceComponentStatusRequest(Space space, SpaceComponentTypeFB componentType, bool enabled)
        {
            var info = new SpaceComponentStatusSetInfoFB
            {
                Type = StructureType.SpaceComponentStatusSetInfoFB,
                ComponentType = componentType,
                Enabled = (uint)(enabled ? 1 : 0),
            };

            ulong reqId = (ulong)new DateTime().Ticks;

            _app!.CheckResult(_spatial!.SetSpaceComponentStatusFB(space, in info, ref reqId), "SetSpaceComponentStatusFB");

            return reqId;
        }

        protected Task<T> SubmitQuery<T>(string hash, Func<ulong> action)
        {
            lock (_queries)
            {
                if (!_queries.TryGetValue(hash, out var query))
                {
                    var reqId = action();

                    query = new ActiveQuery
                    {
                        ReqId = reqId,
                        Completion = new TaskCompletionSource<T>(),
                        Hash = hash
                    };

                    _queries[hash] = query;
                }
                return ((TaskCompletionSource<T>)query.Completion!).Task;
            }
        }

        protected TaskCompletionSource<T>? QueryCompletion<T>(ulong reqId)
        {
            lock (_queries)
            {
                var query = _queries.Values.FirstOrDefault(a => a.ReqId == reqId);

                if (query == null)
                    return null;

                _queries.Remove(query.Hash!);

                return (TaskCompletionSource<T>?)query.Completion;
            }
        }

        public unsafe SpaceComponentTypeFB[] EnumerateSpaceSupportedComponentsFB(Space space)
        {
            uint count;

            var result = new SpaceComponentTypeFB[10];

            _app!.CheckResult(_spatial!.EnumerateSpaceSupportedComponentsFB(space, &count, result), "EnumerateSpaceSupportedComponentsFB");

            Array.Resize(ref result, (int)count);

            return result;
        }

        protected unsafe ulong QueryAllAnchorsRequest(Guid[]? ids)
        {
            var hasIds = ids != null && ids.Length > 0;

            ids ??= new Guid[1];

            fixed (Guid* uuids = &ids[0])
            {
                var filter = new SpaceUuidFilterInfoFB();   

                if (hasIds)
                {
                    filter.Uuids = (UuidEXT*)uuids;
                    filter.UuidCount = (uint)ids.Length;
                    filter.Type = StructureType.SpaceUuidFilterInfoFB;
                }

                var query = new SpaceQueryInfoFB()
                {
                    Type = StructureType.SpaceQueryInfoFB,
                    QueryAction = SpaceQueryActionFB.LoadFB,
                    Filter = hasIds ? (SpaceFilterInfoBaseHeaderFB*)&filter : null,
                    MaxResultCount = 100,
                };

                ulong reqId = (ulong)new DateTime().Ticks;

                _app!.CheckResult(_spatialQuery!.QuerySpacesFB(_app!.Session, (SpaceQueryInfoBaseHeaderFB*)&query, ref reqId), "QuerySpacesFB");

                return reqId;
            }
        }

        protected unsafe SpaceQueryResultFB[] GetSpaceQueryResults(ulong reqId)
        {
            var result = new SpaceQueryResultsFB()
            {
                Type = StructureType.SpaceQueryResultsFB,
            };

            _app!.CheckResult(_spatialQuery!.RetrieveSpaceQueryResultsFB(_app!.Session, reqId, ref result), "RetrieveSpaceQueryResultsFB");

            var results = new SpaceQueryResultFB[(int)result.ResultCountOutput];

            fixed (SpaceQueryResultFB* ptr = results)
            {
                result.ResultCapacityInput = result.ResultCountOutput;
                result.Results = ptr;
                _app!.CheckResult(_spatialQuery!.RetrieveSpaceQueryResultsFB(_app!.Session, reqId, ref result), "RetrieveSpaceQueryResultsFB");
            }

            Array.Resize(ref results, (int)result.ResultCountOutput);

            return results;
        }

        public unsafe Guid[] GetSpaceContainer(Space space)
        {
            var result = new SpaceContainerFB
            {
                Type = StructureType.SpaceContainerFB
            };

            _app!.CheckResult(_container!.GetSpaceContainerFB(_app!.Session, space, ref result), "GetSpaceContainerFB");

            var uuids = stackalloc UuidEXT[(int)result.UuidCountOutput];

            result.Uuids = &uuids[0];
            result.UuidCapacityInput = result.UuidCountOutput;

            _app!.CheckResult(_container.GetSpaceContainerFB(_app!.Session, space, ref result), "GetSpaceContainerFB");

            return new Span<UuidEXT>(uuids, (int)result.UuidCountOutput)
                .ToArray()
                .Select(a => a.ToGuid())
                .ToArray();

        }

        public unsafe Mesh GetSpaceTriangleMesh(Space space)
        {
            var info = new SpaceTriangleMeshGetInfoMETA
            {
                Type = XR_TYPE_SPACE_TRIANGLE_MESH_GET_INFO_META
            };

            var result = new SpaceTriangleMeshMETA
            {
                Type = XR_TYPE_SPACE_TRIANGLE_MESH_META
            };

            _app!.CheckResult(GetSpaceTriangleMeshMETA!(space, ref info, ref result), "GetSpaceTriangleMeshMETA");

            var vertexArray = new Vector3f[result.VertexCountOutput];
            var indexArray = new uint[result.IndexCountOutput];

            fixed (Vector3f* pVertex = vertexArray)
            fixed (uint* pIndex = indexArray)
            {
                result.VertexCapacityInput = result.VertexCountOutput;
                result.Vertices = pVertex;
                result.IndexCapacityInput = result.IndexCountOutput;
                result.Indices = pIndex;
                _app!.CheckResult(GetSpaceTriangleMeshMETA!(space, ref info, ref result), "GetSpaceTriangleMeshMETA");

                return new Mesh
                {
                    Vertices = vertexArray.Convert().To<Vector3>(),
                    Indices = indexArray
                };
            }
        }

        public IEnumerable<SpaceQueryResultFB> SpaceWithComponents(IEnumerable<SpaceQueryResultFB> spaces, params SpaceComponentTypeFB[] componets)
        {
            foreach (var space in spaces)
            {
                var caps = EnumerateSpaceSupportedComponentsFB(space.Space);
                if (componets.All(a => caps.Contains(a)))
                    yield return space;
            }
        }

        public Task<Result> SetSpaceComponentStatusAsync(Space space, SpaceComponentTypeFB componentType, bool enabled)
        {
            var hash = $"{space.Handle}_{componentType}";

            return SubmitQuery<Result>(hash, () =>
                       SetSpaceComponentStatusRequest(space, componentType, enabled));
        }

        public Task<SpaceQueryResultFB[]> QueryAllAnchorsAsync(Guid[]? ids = null)
        {
            return SubmitQuery<SpaceQueryResultFB[]>("AllAnchors", () => QueryAllAnchorsRequest(ids));
        }

        public override void HandleEvent(ref EventDataBuffer buffer)
        {
            var test = buffer.Type.ToString();
            Debug.WriteLine(test);

            if (buffer.Type == StructureType.EventDataSpaceQueryCompleteFB)
            {
                var data = buffer.Convert().To<EventDataSpaceQueryCompleteFB>();

                var query = QueryCompletion<Result>(data.RequestId);

                query?.ScheduleCancel(TimeSpan.FromSeconds(5));
            }


            else if (buffer.Type == StructureType.EventDataSpaceSetStatusCompleteFB)
            {
                var data = buffer.Convert().To<EventDataSpaceSetStatusCompleteFB>();

                var query = QueryCompletion<Result>(data.RequestId);

                query?.SetResult(data.Result);
            }

            else if (buffer.Type == StructureType.EventDataSpaceQueryResultsAvailableFB)
            {
                var data = buffer.Convert().To<EventDataSpaceQueryResultsAvailableFB>();

                var query = QueryCompletion<SpaceQueryResultFB[]>(data.RequestId);

                try
                {
                    var result = GetSpaceQueryResults(data.RequestId);
                    query?.SetResult(result);
                }
                catch (Exception ex)
                {
                    query?.SetException(ex);
                }
            }

            else if (buffer.Type == StructureType.EventDataSpatialAnchorCreateCompleteFB)
            {
                var data = buffer.Convert().To<EventDataSpatialAnchorCreateCompleteFB>();

            }

            else if (buffer.Type == StructureType.EventDataSpaceSaveCompleteFB)
            {
                var data = buffer.Convert().To<EventDataSpaceSaveCompleteFB>();

            }
            else if (buffer.Type == StructureType.EventDataSpaceEraseCompleteFB)
            {
                var data = buffer.Convert().To<EventDataSpaceEraseCompleteFB>();
            }
        }

        public unsafe RoomLayoutFB GetSpaceRoomLayout(Space space)
        {
            var result = new RoomLayoutFB
            {
                Type = StructureType.RoomLayoutFB
            };

            _app!.CheckResult(_scene!.GetSpaceRoomLayoutFB(_app!.Session, space, ref result), "GetSpaceRoomLayoutFB");

            var walls = new UuidEXT[result.WallUuidCountOutput];

            fixed (UuidEXT* wallPtr = walls)
            {
                result.WallUuidCapacityInput = (uint)walls.Length;
                result.WallUuids = wallPtr;
                _app!.CheckResult(_scene!.GetSpaceRoomLayoutFB(_app!.Session, space, ref result), "GetSpaceRoomLayoutFB");
            }

            return result;
        }

        public unsafe void UpdateFoveation(FoveationDynamicFB dynamic, FoveationLevelFB level, float offset)
        {
            var create = new FoveationProfileCreateInfoFB()
            {
                Type = StructureType.FoveationProfileCreateInfoFB
            };

            var info = new FoveationLevelProfileCreateInfoFB
            {
                Type = StructureType.FoveationLevelProfileCreateInfoFB,
                Dynamic = dynamic,
                Level = level,
                VerticalOffset = offset
            };

            create.Next = &info;

            var profile = new FoveationProfileFB();

            _app!.CheckResult(_foveation!.CreateFoveationProfileFB(_app!.Session, in create, ref profile), "CreateFoveationProfileFB");

            var update = new SwapchainStateFoveationFB
            {
                Type = StructureType.SwapchainStateFoveationFB,
                Profile = profile,
                Flags = SwapchainStateFoveationFlagsFB.None,
            };

            foreach (var proj in _app.Layers.List.OfType<XrProjectionLayer>())
            {
                foreach (var swapChain in proj.ColorSwapChains)
                    _app.CheckResult(_swapChainUpdate!.UpdateSwapchainFB(swapChain, (SwapchainStateBaseHeaderFB*)&update), "UpdateSwapchainFB");
            }

            _app!.CheckResult(_foveation!.DestroyFoveationProfileFB(profile), "DestroyFoveationProfileFB");
        }

        public unsafe override void ConfigureSwapchain(ref SwapchainCreateInfo info)
        {
            if (_options.Foveation == SwapchainCreateFoveationFlagsFB.None)
                return;

            _foveationInfo.Value = new SwapchainCreateInfoFoveationFB
            {
                Type = StructureType.SwapchainCreateInfoFoveationFB,
                Flags = _options.Foveation
            };

            ref BaseInStructure curInput = ref Unsafe.As<SwapchainCreateInfo, BaseInStructure>(ref info);

            while (curInput.Next != null)
                curInput = ref Unsafe.AsRef<BaseInStructure>(curInput.Next);

            curInput.Next = (BaseInStructure*)_foveationInfo.Pointer;
        }

        public unsafe float GetSampleRate(Action action, ulong subActionPath = 0)
        {
            var info = new HapticActionInfo(StructureType.HapticActionInfo)
            {
                Action = action,
                SubactionPath = subActionPath
            };

            var res = new DevicePcmSampleRateGetInfoFB(StructureType.DevicePcmSampleRateGetInfoFB);

            _app!.CheckResult(_haptic!.GetDeviceSampleRateFB(_app!.Session, in info, ref res), "GetDeviceSampleRateFB");

            return res.SampleRate;
        }

        public unsafe uint ApplyVibrationPcmFeedback(Action action, Span<float> buffer, float sampleRate, bool append, ulong subActionPath = 0)
        {
            var info = new HapticActionInfo(StructureType.HapticActionInfo)
            {
                Action = action,
                SubactionPath = subActionPath
            };

            uint result = 0;

            fixed (float* pBuffer = buffer)
            {
                var vibration = new HapticPcmVibrationFB(StructureType.HapticPcmVibrationFB)
                {
                    SamplesConsumed = &result,
                    SampleRate = sampleRate,
                    Append = (uint)(append ? 1 : 0),
                    BufferSize = (uint)buffer.Length,
                    Buffer = pBuffer
                };

                _app!.CheckResult(_app.Xr.ApplyHapticFeedback(_app.Session!, in info, (HapticBaseHeader*)&vibration), "ApplyHapticFeedback");
            }
            return result;
        }

        public unsafe float[] EnumerateDisplayRefreshRates()
        {
            uint count = 0;
            _app!.CheckResult(_refreshRate!.EnumerateDisplayRefreshRatesFB(_app!.Session, 0, ref count, null), "EnumerateDisplayRefreshRates");

            var result = new float[(int)count];

            fixed (float* pResult = result)
                _app!.CheckResult(_refreshRate!.EnumerateDisplayRefreshRatesFB(_app!.Session, count, ref count, pResult), "EnumerateDisplayRefreshRates");

            return result;
        }

        public void RequestDisplayRefreshRate(float value)
        {

            _app!.CheckResult(_refreshRate!.RequestDisplayRefreshRateFB(_app!.Session, value), "RequestDisplayRefreshRate");
        }

        public unsafe TriangleMeshFB CreateTriangleMesh(uint[] indices, Vector3f[] vertices)
        {
            if (indices.Length == 0)
            {
                indices = new uint[vertices.Length];
                for (uint i = 0; i < indices.Length; ++i)
                    indices[i] = i;
            }

            fixed (uint* pIndices = indices)

            fixed (Vector3f* pVertices = vertices)
                return CreateTriangleMesh(pIndices, (uint)indices.Length, pVertices, (uint)vertices.Length);
        }

        public unsafe TriangleMeshFB CreateTriangleMesh(uint* indices, uint indicesCount, Vector3f* vertices, uint verticesCount)
        {
            var info = new TriangleMeshCreateInfoFB
            {
                Type = StructureType.TriangleMeshCreateInfoFB,
                Flags = TriangleMeshFlagsFB.None,
                IndexBuffer = indices,
                TriangleCount = indicesCount / 3,
                VertexBuffer = vertices,
                VertexCount = verticesCount,
                WindingOrder = WindingOrderFB.CcwFB
            };

            var result = new TriangleMeshFB();

            _app!.CheckResult(_mesh!.CreateTriangleMeshFB(_app!.Session, in info, ref result), "CreateTriangleMeshFB");

            return result;
        }

        public unsafe XrHandMesh GetHandMesh(HandTrackerEXT tracker)
        {
            var mesh = new HandTrackingMeshFB
            {
                Type = StructureType.HandTrackingMeshFB
            };

            var capState = new HandTrackingCapsulesStateFB()
            {
                Type = StructureType.HandTrackingCapsulesStateFB,
            };

            _app!.CheckResult(_handMesh!.GetHandMeshFB(tracker, ref mesh), "GetHandMeshFB");

            var jointBindPoses = new Posef[mesh.JointCountOutput];
            var jointParents = new HandJointEXT[mesh.JointCountOutput];
            var jointRadii = new float[mesh.JointCountOutput];

            var vertexPositions = new Vector3f[mesh.VertexCountOutput];
            var vertexNormals = new Vector3f[mesh.VertexCountOutput];
            var vertexUVs = new Vector2f[mesh.VertexCountOutput];
            var vertexBlendIndices = new Vector4D<short>[mesh.VertexCountOutput];
            var vertexBlendWeights = new Vector4f[mesh.VertexCountOutput];

            var indices = new short[mesh.IndexCountOutput];

            fixed (Posef* pjointBindPoses = jointBindPoses)
            fixed (HandJointEXT* pjointParents = jointParents)
            fixed (float* pjointRadii = jointRadii)
            fixed (Vector3f* pvertexPositions = vertexPositions)
            fixed (Vector3f* pvertexNormals = vertexNormals)
            fixed (Vector2f* pvertexUVs = vertexUVs)
            fixed (Vector4D<short>* pvertexBlendIndices = vertexBlendIndices)
            fixed (Vector4f* pvertexBlendWeights = vertexBlendWeights)
            fixed (short* pindices = indices)
            {
                mesh.JointBindPoses = pjointBindPoses;
                mesh.JointParents = pjointParents;
                mesh.JointRadii = pjointRadii;
                mesh.JointCapacityInput = mesh.JointCountOutput;

                mesh.VertexPositions = pvertexPositions;
                mesh.VertexNormals = pvertexNormals;
                mesh.VertexUVs = pvertexUVs;
                mesh.VertexBlendIndices = pvertexBlendIndices;
                mesh.VertexBlendWeights = pvertexBlendWeights;
                mesh.VertexCapacityInput = mesh.VertexCountOutput;

                mesh.Indices = pindices;
                mesh.IndexCapacityInput = mesh.IndexCountOutput;

                mesh.Next = &capState;

                _app!.CheckResult(_handMesh!.GetHandMeshFB(tracker, ref mesh), "GetHandMeshFB");
            }

            var result = new XrHandMesh();
            result.Joints = new XrHandJoint[mesh.JointCountOutput];
            result.Vertices = new XrHandVertex[mesh.VertexCountOutput];
            result.Indices = new uint[mesh.IndexCountOutput];

            for (var i = 0; i < mesh.JointCountOutput; i++)
            {
                result.Joints[i].BindPose = jointBindPoses[i];
                result.Joints[i].Parent = jointParents[i];
                result.Joints[i].Radii = jointRadii[i];
            }

            for (var i = 0; i < mesh.VertexCountOutput; i++)
            {
                result.Vertices[i].Pos = vertexPositions[i];
                result.Vertices[i].Normal = vertexNormals[i];
                result.Vertices[i].UV = vertexUVs[i];
                result.Vertices[i].BlendIndex = vertexBlendIndices[i];
                result.Vertices[i].BlendWeight = vertexBlendWeights[i];
            }

            for (var i = 0; i < mesh.IndexCountOutput; i++)
                result.Indices[i] = (uint)indices[i];

            result.Capsules = capState.Capsules.AsSpan().ToArray();

            return result;
        }

        public void Dispose()
        {
            _foveationInfo.Dispose();
            GC.SuppressFinalize(this);
        }

        public OculusXrPluginOptions Options => _options;


    }
}
