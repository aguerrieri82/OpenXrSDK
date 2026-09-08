namespace Oculus.Platform;

/// <summary>Explicit platform initialization for a .NET application.</summary>
public static class Core
{
    private static bool _initialized;
    public static bool LogMessages;
    public const string PlatformUninitializedError = "Initialize Oculus.Platform.Core before making platform requests.";
    public static bool IsInitialized() => _initialized;

    /// <summary>Initialize Windows or standalone PC mode. For Quest use the Activity overload.</summary>
    public static HorizonStatus Initialize(string appId, string runtimeMode = "windows2", string accessToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        if (runtimeMode is not ("windows" or "standalone"))
            throw new ArgumentException("Expected windows or standalone.", nameof(runtimeMode));
        if (runtimeMode == "standalone") ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("This overload requires 64-bit Windows.");
        var status = WindowsClient.Initialize(appId, runtimeMode, accessToken);
        status.ThrowIfError();
        _initialized = true;
        return status;
    }

    public static Task<HorizonStatus> InitializeAsync(string appId, string runtimeMode = "windows", string accessToken = null)
        => Task.Run(() => Initialize(appId, runtimeMode, accessToken));

#if ANDROID
    /// <summary>Call on the Android activity thread after Horizon supplements are available.</summary>
    public static HorizonStatus Initialize(global::Android.App.Activity activity, string appId)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        var status = AndroidClient.Initialize(activity, appId);
        status.ThrowIfError();
        _initialized = true;
        return status;
    }
#endif
}
