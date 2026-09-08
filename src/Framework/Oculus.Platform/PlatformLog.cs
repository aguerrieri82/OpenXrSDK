namespace Oculus.Platform;

/// <summary>Optional engine logger. Message payload logging is disabled by default.</summary>
public static class PlatformLog
{
    public static Action<string> Logger { get; set; }
    public static void Log(object value) => Logger?.Invoke(value?.ToString() ?? "");
    public static void LogWarning(object value) => Log(value);
    public static void LogError(object value) => Log(value);
}
