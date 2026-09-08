using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using global::Oculus.Avatar2;
using AvatarApi = global::Oculus.Avatar2.CAPI;

namespace XrEngine.OpenXr.Oculus
{
    // One instance, used as: await LoginAsync(...), await LoadAsync(...), Dispose().
    public partial class OculusAvatar : IDisposable
    {
        private readonly BlockingCollection<Action> _commands = new();
        private readonly ConcurrentQueue<AvatarApi.ovrAvatar2Asset_Resource> _resources = new();
        private readonly Dictionary<AvatarApi.ovrAvatar2Id, PrimitiveData> _primitives = new();
        private readonly Dictionary<AvatarApi.ovrAvatar2Id, ImageData> _images = new();
        private readonly AvatarApi.ResourceDelegate _resourceCallback;
        private Thread? _thread;
        private bool _initialized;
        private AvatarApi.ovrAvatar2EntityId _entity = AvatarApi.ovrAvatar2EntityId.Invalid;
        private AvatarApi.ovrAvatar2LoadRequestId _requestId;
        private TaskCompletionSource<Group3D>? _load;
        private readonly Stopwatch _loadTime = new();

        public OculusAvatar()
        {
            _resourceCallback = OnResource;
        }

        public Task LoginAsync(string accessToken)
        {
            return InvokeAsync(() =>
            {
                if (!_initialized)
                {
                    var platform = OperatingSystem.IsAndroid()
                        ? AvatarApi.ovrAvatar2Platform.Quest
                        : AvatarApi.ovrAvatar2Platform.PC;

                    var init = AvatarDefaults.CreateInitializeInfo(platform, "XrEngine");
                    init.resourceLoadCallback = _resourceCallback;
                    Check(AvatarApi.ovrAvatar2_Initialize(in init));
                    _initialized = true;
                }

                Check(AvatarDefaults.SetAccessToken(accessToken));
                return true;
            });
        }

        public Task<Group3D> LoadAsync(string userId)
        {
            return InvokeAsync(() => BeginLoad(ulong.Parse(userId))).Unwrap();
        }

        private Task<Group3D> BeginLoad(ulong userId)
        {
            var filters = AvatarDefaults.FullBodyFilters;
            filters.lodFlags = AvatarApi.ovrAvatar2EntityLODFlags.LOD_0;
            filters.viewFlags = AvatarApi.ovrAvatar2EntityViewFlags.ThirdPerson;

            var create = new AvatarApi.ovrAvatar2EntityCreateInfo
            {
                features = AvatarApi.ovrAvatar2EntityFeatures.Rendering,
                renderFilters = filters
            };

            Check(AvatarApi.ovrAvatar2Entity_Create(in create, out _entity));

            try
            {
                var settings = AvatarApi.ovrAvatar2Entity_DefaultLoadSettings();
                settings.loadFilters = filters;
                settings.prefetchOnly = false;

                var result = AvatarApi.ovrAvatar2Entity_LoadUserFromGraph(
                    _entity, userId, AvatarApi.ovrAvatar2Graph.Oculus, settings, out _requestId);

                if (result != AvatarApi.ovrAvatar2Result.Pending)
                    Check(result);

                _load = new TaskCompletionSource<Group3D>(TaskCreationOptions.RunContinuationsAsynchronously);
                _loadTime.Restart();
                return _load.Task;
            }
            catch
            {
                ReleaseEntity();
                throw;
            }
        }

        private Task<T> InvokeAsync<T>(Func<T> action)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            _commands.Add(() =>
            {
                try
                {
                    completion.SetResult(action());
                }
                catch (Exception error)
                {
                    completion.SetException(error);
                }
            });

            if (_thread == null)
            {
                _thread = new Thread(UpdateLoop)
                {
                    IsBackground = true,
                    Name = "Oculus Avatar loading"
                };
                _thread.Start();
            }

            return completion.Task;
        }

        private void UpdateLoop()
        {
            var clock = Stopwatch.StartNew();
            var previous = clock.Elapsed;

            try
            {
                while (!_commands.IsCompleted)
                {
                    if (_commands.TryTake(out var command, 10))
                        command();

                    var now = clock.Elapsed;
                    var delta = (float)(now - previous).TotalSeconds;
                    previous = now;

                    if (_load == null)
                        continue;

                    try
                    {
                        Check(AvatarApi.ovrAvatar2_Update(delta));
                        ReadResources();
                        Check(AvatarApi.ovrAvatar2Asset_GetLoadRequestInfo(_requestId, out var info));

                        if (info.state == AvatarApi.ovrAvatar2LoadRequestState.Failed)
                            throw new InvalidOperationException($"Avatar load failed: {info.failedReason}, HTTP {info.responseCode}.");

                        if (info.state == AvatarApi.ovrAvatar2LoadRequestState.Cancelled)
                            throw new OperationCanceledException("Avatar load cancelled.");

                        if (_loadTime.Elapsed > TimeSpan.FromSeconds(90))
                            throw new TimeoutException("Avatar loading exceeded 90 seconds.");

                        if (info.state == AvatarApi.ovrAvatar2LoadRequestState.Success)
                        {
                            var avatar = BuildAvatar();
                            var completion = _load;
                            ReleaseEntity();
                            _load = null;
                            completion.SetResult(avatar);
                        }
                    }
                    catch (Exception error)
                    {
                        var completion = _load;
                        ReleaseEntity();
                        _load = null;
                        completion?.TrySetException(error);
                    }
                }
            }
            finally
            {
                ReleaseEntity();
                _load?.TrySetCanceled();

                if (_initialized)
                    AvatarApi.ovrAvatar2_Shutdown();

                _primitives.Clear();
                _images.Clear();
                GC.KeepAlive(_resourceCallback);
            }
        }

        private void OnResource(in AvatarApi.ovrAvatar2Asset_Resource resource, IntPtr context)
        {
            _resources.Enqueue(resource);
        }

        private void ReadResources()
        {
            while (_resources.TryDequeue(out var resource))
            {
                if (resource.status == AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_LoadFailed)
                    throw new InvalidOperationException($"Avatar resource {resource.assetID} failed to load.");

                if (resource.status != AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_Loaded &&
                    resource.status != AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_Updated)
                    continue;

                try
                {
                    ReadImages(resource.assetID);
                    Check(AvatarApi.ovrAvatar2Asset_GetPrimitiveCount(resource.assetID, out var count));

                    for (uint i = 0; i < count; i++)
                    {
                        Check(AvatarApi.ovrAvatar2Asset_GetPrimitiveByIndex(resource.assetID, i, out var primitive));
                        _primitives[primitive.id] = ReadPrimitive(primitive);
                    }

                    Check(AvatarApi.ovrAvatar2Asset_ResourceReadyToRender(resource.assetID));
                }
                finally
                {
                    AvatarApi.ovrAvatar2Asset_ReleaseResource(resource.assetID);
                }
            }
        }

        private void ReleaseEntity()
        {
            if (_entity != AvatarApi.ovrAvatar2EntityId.Invalid)
            {
                AvatarApi.ovrAvatar2Entity_Destroy(_entity);
                _entity = AvatarApi.ovrAvatar2EntityId.Invalid;
            }

            while (_resources.TryDequeue(out var resource))
            {
                if (resource.status == AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_Loaded ||
                    resource.status == AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_Updated)
                    AvatarApi.ovrAvatar2Asset_ReleaseResource(resource.assetID);
            }

        }

        private static void Check(AvatarApi.ovrAvatar2Result result)
        {
            if (result != AvatarApi.ovrAvatar2Result.Success)
                throw new InvalidOperationException($"Avatar SDK: {result}.");
        }

        public void Dispose()
        {
            _commands.CompleteAdding();
            _thread?.Join();
            _commands.Dispose();
        }
    }
}
