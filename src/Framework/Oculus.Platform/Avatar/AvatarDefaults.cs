using System.Text;

namespace Oculus.Avatar2;

/// <summary>Convenience values for the native binding; owns no runtime or renderer.</summary>
public static class AvatarDefaults
{
    /// <summary>Uses native Avatar coordinates: +X right, +Y up, -Z forward.</summary>
    public static CAPI.ovrAvatar2InitializeInfo CreateInitializeInfo(
        CAPI.ovrAvatar2Platform platform, string clientName, string clientVersion = "1.0")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        if (!Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("Only Windows x64 and Android ARM64 are supported.");
        return new CAPI.ovrAvatar2InitializeInfo
        {
            versionNumber = AvatarSDKVersion.CurrentVersion(),
            clientName = clientName,
            clientVersion = clientVersion,
            platform = platform,
            flags = CAPI.ovrAvatar2InitializeFlags.UseDefaultImage,
            loggingLevel = CAPI.ovrAvatar2LogLevel.Warn,
            shouldCreateWorkerThreads = 1,
            maxNetworkRequests = -1,
            maxNetworkSendBytesPerSecond = -1,
            maxNetworkReceiveBytesPerSecond = -1,
            defaultModelColor = new() { x = 30f / 255, y = 157f / 255, z = 1 },
            clientSpaceRightAxis = new() { x = 1 },
            clientSpaceUpAxis = new() { y = 1 },
            clientSpaceForwardAxis = new() { z = -1 },
            networkWorkerUpdateFrequency = CAPI.DefaultNetworkWorkerUpdateFrequency
        };
    }

    public static CAPI.ovrAvatar2EntityFilters FullBodyFilters => new()
    {
        lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
        manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Full,
        viewFlags = CAPI.ovrAvatar2EntityViewFlags.All,
        subMeshVertexPreference = CAPI.ovrAvatar2EntitySubMeshVertexPreference.Default,
        subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All,
        quality = CAPI.ovrAvatar2EntityQuality.Light,
        loadRigZipFromGlb = true
    };

    /// <summary>Pass the token obtained through Oculus.Platform.Users.GetAccessToken().</summary>
    public static unsafe CAPI.ovrAvatar2Result SetAccessToken(
        string token, CAPI.ovrAvatar2Graph graph = CAPI.ovrAvatar2Graph.Oculus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        if (token.Contains('\0')) throw new ArgumentException("Token contains a null character.", nameof(token));
        var bytes = Encoding.UTF8.GetBytes(token + "\0");
        try
        {
            fixed (byte* pointer = bytes)
                return CAPI.ovrAvatar2_UpdateAccessTokenForGraph(pointer, graph);
        }
        finally { Array.Clear(bytes); }
    }

    /// <summary>Starts an asynchronous native load. Pending is a normal return value.</summary>
    public static CAPI.ovrAvatar2Result LoadUser(CAPI.ovrAvatar2EntityId entity, ulong userId,
        out CAPI.ovrAvatar2LoadRequestId requestId,
        CAPI.ovrAvatar2Graph graph = CAPI.ovrAvatar2Graph.Oculus)
    {
        if (userId == 0) throw new ArgumentOutOfRangeException(nameof(userId));
        var settings = CAPI.ovrAvatar2Entity_DefaultLoadSettings();
        settings.loadFilters = FullBodyFilters;
        settings.prefetchOnly = false;
        return CAPI.ovrAvatar2Entity_LoadUserFromGraph(entity, userId, graph, settings, out requestId);
    }
}
