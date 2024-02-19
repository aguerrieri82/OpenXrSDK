using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Action = Silk.NET.OpenXR.Action;


namespace OpenXr.Framework.Oculus
{

    public class OculusXrPluginOptions
    {
        public bool EnableMultiView { get; set; }

        public SwapchainCreateFoveationFlagsFB Foveation { get; set; }

        public uint SampleCount { get; set; }

        public float ResolutionScale { get; set; }

        public static readonly OculusXrPluginOptions Default = new()
        {
            SampleCount = 2,
            ResolutionScale = 1.0f,
            EnableMultiView = true,
            Foveation = SwapchainCreateFoveationFlagsFB.ScaledBinBitFB
        };
    }


    public unsafe class OculusXrPlugin : BaseXrPlugin, IDisposable
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

        protected FBScene? _scene;
        protected FBSpatialEntity? _spatial;
        protected FBSpatialEntityQuery? _spatialQuery;
        protected FBTriangleMesh? _mesh;
        protected NativeStruct<SwapchainCreateInfoFoveationFB> _foveationInfo;
        protected FBHapticPcm? _haptic;
        protected FBSpatialEntityContainer? _container;
        protected readonly ConcurrentDictionary<ulong, TaskCompletionSource<SpaceQueryResultFB[]>> _spaceQueries = [];
        protected readonly ConcurrentDictionary<ulong, TaskCompletionSource<Result>> _spaceCompStatus = [];


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
            extensions.Add("XR_META_spatial_entity_mesh");
        }

        public override void OnInstanceCreated()
        {

            _app!.Xr.TryGetInstanceExtension<FBScene>(null, _app.Instance, out _scene);
            _app.Xr.TryGetInstanceExtension<FBSpatialEntity>(null, _app.Instance, out _spatial);
            _app.Xr.TryGetInstanceExtension<FBSpatialEntityQuery>(null, _app.Instance, out _spatialQuery);
            _app.Xr.TryGetInstanceExtension<FBTriangleMesh>(null, _app.Instance, out _mesh);
            _app.Xr.TryGetInstanceExtension<FBHapticPcm>(null, _app.Instance, out _haptic);
            _app.Xr.TryGetInstanceExtension<FBSpatialEntityContainer>(null, _app.Instance, out _container);

            var func = new PfnVoidFunction();
            _app.CheckResult(_app.Xr.GetInstanceProcAddr(_app.Instance, "xrGetSpaceTriangleMeshMETA", &func), "Bind xrGetSpaceTriangleMeshMETA");
            GetSpaceTriangleMeshMETA = Marshal.GetDelegateForFunctionPointer<GetSpaceTriangleMeshMETADelegate>(new nint(func.Handle));
        }

        public string[] GetSpaceSemanticLabels(Space space)
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

            ulong requestId = 0;

            _app!.CheckResult(_spatial!.SetSpaceComponentStatusFB(space, in info, ref requestId), "SetSpaceComponentStatusFB");

            return requestId;
        }

        public Task<Result> SetSpaceComponentStatusAsync(Space space, SpaceComponentTypeFB componentType, bool enabled)
        {
            var reqId = SetSpaceComponentStatusRequest(space, componentType, enabled);

            _spaceCompStatus[reqId] = new TaskCompletionSource<Result>();

            return _spaceCompStatus[reqId].Task;
        }


        public SpaceComponentTypeFB[] EnumerateSpaceSupportedComponentsFB(Space space)
        {
            uint count;

            var result = new SpaceComponentTypeFB[10];

            _app!.CheckResult(_spatial!.EnumerateSpaceSupportedComponentsFB(space, &count, result), "EnumerateSpaceSupportedComponentsFB");

            Array.Resize(ref result, (int)count);

            return result;
        }

        protected ulong QueryAllAnchorsRequest()
        {
            var query = new SpaceQueryInfoFB()
            {
                Type = StructureType.SpaceQueryInfoFB,
                QueryAction = SpaceQueryActionFB.LoadFB,
                MaxResultCount = 100,
            };

            ulong requestId = 0;

            _app!.CheckResult(_spatialQuery!.QuerySpacesFB(_app!.Session, (SpaceQueryInfoBaseHeaderFB*)&query, ref requestId), "QuerySpacesFB");

            return requestId;
        }

        protected SpaceQueryResultFB[] GetSpaceQueryResults(ulong reqId)
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

        public Guid[] GetSpaceContainer(Space space)
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

        public XrTriangleMesh GetSpaceTriangleMesh(Space space)
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

                return new XrTriangleMesh
                {
                    Vertices = vertexArray,
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

        public Task<SpaceQueryResultFB[]> QueryAllAnchorsAsync()
        {
            var reqId = QueryAllAnchorsRequest();

            _spaceQueries[reqId] = new TaskCompletionSource<SpaceQueryResultFB[]>();

            return _spaceQueries[reqId].Task;
        }

        public override void HandleEvent(ref EventDataBuffer buffer)
        {
            var test = buffer.Type.ToString();
            Debug.WriteLine(test);

            if (buffer.Type == StructureType.EventDataSpaceQueryCompleteFB)
            {
                var data = buffer.Convert().To<EventDataSpaceQueryCompleteFB>();
                if (_spaceQueries.TryRemove(data.RequestId, out var task))
                    task.ScheduleCancel(TimeSpan.FromSeconds(5));
            }


            else if (buffer.Type == StructureType.EventDataSpaceSetStatusCompleteFB)
            {
                var data = buffer.Convert().To<EventDataSpaceSetStatusCompleteFB>();
                if (_spaceCompStatus.TryRemove(data.RequestId, out var task))
                {
                    task.SetResult(data.Result);
                }
            }

            else if (buffer.Type == StructureType.EventDataSpaceQueryResultsAvailableFB)
            {
                var data = buffer.Convert().To<EventDataSpaceQueryResultsAvailableFB>();

                if (_spaceQueries.TryRemove(data.RequestId, out var task))
                {
                    try
                    {
                        var result = GetSpaceQueryResults(data.RequestId);
                        task.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        task.SetException(ex);
                    }
                }
            }
        }

        public RoomLayoutFB GetSpaceRoomLayout(Space space)
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

        public override void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {
            result.Size = new Extent2Di
            {
                Height = (int)(result.Size.Height * _options.ResolutionScale),
                Width = (int)(result.Size.Width * _options.ResolutionScale),
            };
        }

        public override void ConfigureSwapchain(ref SwapchainCreateInfo info)
        {
            if (_options.EnableMultiView)
                info.ArraySize = 2;

            info.SampleCount = _options.SampleCount;

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

        public float GetSampleRate(Action action, ulong subActionPath = 0)
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

        public uint ApplyVibrationPcmFeedback(Action action, Span<float> buffer, float sampleRate, bool append, ulong subActionPath = 0)
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

        public void Dispose()
        {
            _foveationInfo.Dispose();
            GC.SuppressFinalize(this);
        }

        public OculusXrPluginOptions Options => _options;


    }
}
