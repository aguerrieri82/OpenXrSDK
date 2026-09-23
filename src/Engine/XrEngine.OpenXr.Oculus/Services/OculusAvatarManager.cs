using global::Oculus.Avatar2;
using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Diagnostics;
using XrEngine.Gltf;
using AvatarApi = global::Oculus.Avatar2.CAPI;

namespace XrEngine.OpenXr.Oculus
{
    public class OculusAvatarManager : IDisposable
    {
        private readonly BlockingCollection<Action> _commands = new();
        private readonly ConcurrentQueue<AvatarApi.ovrAvatar2Asset_Resource> _resourceQueue = new();
        private readonly OculusAvatarResources _resources = new();
        private readonly OculusAvatarBuilder _builder;
        private readonly AvatarApi.ResourceDelegate _resourceCallback;
        private Thread? _thread;
        private bool _initialized;
        private AvatarApi.ovrAvatar2EntityId _entity = AvatarApi.ovrAvatar2EntityId.Invalid;
        private AvatarApi.ovrAvatar2LoadRequestId _requestId;
        private TaskCompletionSource<Avatar>? _load;
        private readonly Stopwatch _loadTime = new();


        public OculusAvatarManager()
        {
            _resourceCallback = OnResource;
            _builder = new OculusAvatarBuilder(_resources);
            UseCache = true;
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
                    OculusAvatarResources.Check(AvatarApi.ovrAvatar2_Initialize(in init));
                    _initialized = true;
                }

                OculusAvatarResources.Check(AvatarDefaults.SetAccessToken(accessToken));
                return true;
            });
        }

        public Task<Avatar> LoadAsync(string userId)
        {
            return InvokeAsync(() => BeginLoad(ulong.Parse(userId))).Unwrap();
        }

        private async Task<Avatar> BeginLoad(ulong userId)
        {
            string? cacheFile = null;

            if (UseCache)
            {
                cacheFile = Path.Combine(XrPlatform.Current!.CachePath, $"{userId}.avatar_v1.glb");

                if (File.Exists(cacheFile))
                {
                    try
                    {
                        var avatarRaw = (Group3D)GltfLoader.LoadFile(cacheFile);
                        var avatar = new Avatar();
                        foreach (var child in avatarRaw.Children.ToArray())
                            avatar.AddChild(child);
                        return avatar;
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Avatar cache read error:\n{0}", ex.Message);
                    }
                }
            }


            var filters = AvatarDefaults.FullBodyFilters;
            filters.lodFlags = AvatarApi.ovrAvatar2EntityLODFlags.LOD_0;
            filters.viewFlags = AvatarApi.ovrAvatar2EntityViewFlags.ThirdPerson;

            var create = new AvatarApi.ovrAvatar2EntityCreateInfo
            {
                features = AvatarApi.ovrAvatar2EntityFeatures.Rendering,
                renderFilters = filters
            };

            OculusAvatarResources.Check(AvatarApi.ovrAvatar2Entity_Create(in create, out _entity));

            try
            {
                var settings = AvatarApi.ovrAvatar2Entity_DefaultLoadSettings();
                settings.loadFilters = filters;
                settings.prefetchOnly = false;

                var result = AvatarApi.ovrAvatar2Entity_LoadUserFromGraph(
                    _entity, userId, AvatarApi.ovrAvatar2Graph.Oculus, settings, out _requestId);

                if (result != AvatarApi.ovrAvatar2Result.Pending)
                    OculusAvatarResources.Check(result);

                _load = new TaskCompletionSource<Avatar>(TaskCreationOptions.RunContinuationsAsynchronously);
                _loadTime.Restart();

                var avatar = await _load.Task;

                if (cacheFile != null)
                {
                    var exporter = new GltfExporter();
                    await exporter.ExportAsync(avatar, cacheFile);
                }

                return avatar;
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
                        OculusAvatarResources.Check(AvatarApi.ovrAvatar2_Update(delta));
                        ReadResources();
                        OculusAvatarResources.Check(AvatarApi.ovrAvatar2Asset_GetLoadRequestInfo(_requestId, out var info));

                        if (info.state == AvatarApi.ovrAvatar2LoadRequestState.Failed)
                            throw new InvalidOperationException($"Avatar load failed: {info.failedReason}, HTTP {info.responseCode}.");

                        if (info.state == AvatarApi.ovrAvatar2LoadRequestState.Cancelled)
                            throw new OperationCanceledException("Avatar load cancelled.");

                        if (_loadTime.Elapsed > TimeSpan.FromSeconds(90))
                            throw new TimeoutException("Avatar loading exceeded 90 seconds.");

                        if (info.state == AvatarApi.ovrAvatar2LoadRequestState.Success)
                        {
                            var avatar = _builder.Build(_entity);
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

                _resources.Clear();
                GC.KeepAlive(_resourceCallback);
            }
        }

        private void OnResource(in AvatarApi.ovrAvatar2Asset_Resource resource, IntPtr context)
        {
            _resourceQueue.Enqueue(resource);
        }

        private void ReadResources()
        {
            while (_resourceQueue.TryDequeue(out var resource))
                _resources.Read(resource);
        }

        private void ReleaseEntity()
        {
            if (_entity != AvatarApi.ovrAvatar2EntityId.Invalid)
            {
                AvatarApi.ovrAvatar2Entity_Destroy(_entity);
                _entity = AvatarApi.ovrAvatar2EntityId.Invalid;
            }

            while (_resourceQueue.TryDequeue(out var resource))
            {
                if (resource.status == AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_Loaded ||
                    resource.status == AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_Updated)
                    AvatarApi.ovrAvatar2Asset_ReleaseResource(resource.assetID);
            }
        }

        public void Dispose()
        {
            _commands.CompleteAdding();
            _thread?.Join();
            _commands.Dispose();
            GC.SuppressFinalize(this);
        }

        public bool UseCache { get; set; }

    }
}