namespace XrEngine.OpenXr
{
    public interface IAndroidHost
    {
        nint NativeContext { get; }

        nint NativeJniEnv { get; }
    }
}
